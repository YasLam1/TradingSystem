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

    public FinnhubMarketDataFeed(string apiKey) 
        => _apiKey = apiKey;

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
}
