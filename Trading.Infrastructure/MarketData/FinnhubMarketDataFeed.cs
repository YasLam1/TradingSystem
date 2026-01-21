using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Trading.Domain.Entities;
using Trading.Domain.Interfaces;

namespace Trading.Infrastructure.MarketData;

public class FinnhubMarketDataFeed : IMarketDataFeed
{
    private readonly string _apiKey;

    public FinnhubMarketDataFeed() 
        => _apiKey = Environment.GetEnvironmentVariable("FINNHUB_API_KEY")
                     ?? throw new Exception("API key missing");

    public async IAsyncEnumerable<Quote> StartPriceStreamAsync(
        string symbol, [EnumeratorCancellation] CancellationToken ct)
    {
        string FinnhubUrl = $"wss://ws.finnhub.io?token={_apiKey}";

        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri(FinnhubUrl), ct);

        // Subscribe to the symbol
        var subscribeMsg = JsonSerializer.Serialize(new
        {
            type = "subscribe",
            symbol = symbol.ToUpper()
        });

        await ws.SendAsync(
            Encoding.UTF8.GetBytes(subscribeMsg),
            WebSocketMessageType.Text,
            true,
            ct);

        byte[] buffer = new byte[8192];

        while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

            if (result.MessageType == WebSocketMessageType.Close)
                break;

            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("data", out var dataArray) &&
                dataArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in dataArray.EnumerateArray())
                {
                    if (item.TryGetProperty("p", out var priceEl) &&
                        item.TryGetProperty("s", out var symEl))
                    {
                        var price = priceEl.GetDecimal();

                        yield return new Quote
                        {
                            Symbol = symEl.GetString()!,
                            Bid = price,
                            Ask = price,
                            LastTradedPrice = price,
                            Timestamp = DateTime.UtcNow
                        };
                    }
                }
            }
        }
    }

    public async Task<string> GetSymbolFromIsinAsync(
        string isin, CancellationToken ct)
    {
        using var http = new HttpClient();

        string url = $"https://finnhub.io/api/v1/search?q={isin}&token={_apiKey}";

        var json = await http.GetStringAsync(url, ct);

        using var doc = JsonDocument.Parse(json);

        var results = doc.RootElement
            .GetProperty("result")
            .EnumerateArray();

        foreach (var item in results)
        {
            // Prefer exact ISIN match if available
            if (item.TryGetProperty("isin", out var isinEl) &&
                isinEl.GetString() == isin)
            {
                return item.GetProperty("symbol").GetString();
            }
        }

        // fallback: take first result if no isin field exists
        return doc.RootElement
            .GetProperty("result")
            .EnumerateArray()
            .FirstOrDefault()
            .GetProperty("symbol")
            .GetString();
    }
}
