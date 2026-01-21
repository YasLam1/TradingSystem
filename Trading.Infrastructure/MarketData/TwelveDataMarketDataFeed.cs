using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Trading.Domain.Entities;
using Trading.Domain.Interfaces;

namespace Trading.Infrastructure.MarketData;

public class TwelveDataMarketDataFeed : IMarketDataFeed
{
    private readonly string _apiKey;

    public TwelveDataMarketDataFeed()
        => _apiKey = Environment.GetEnvironmentVariable("TwelveData_API_KEY")
                     ?? throw new Exception("API key missing");

    public async IAsyncEnumerable<Quote> StartPriceStreamAsync(
        string symbol, [EnumeratorCancellation] CancellationToken ct)
    {
        string url = $"wss://ws.twelvedata.com/v1/quotes/price?apikey={_apiKey}";

        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri(url), ct);

        // subscribe
        string sub = JsonSerializer.Serialize(new
        {
            action = "subscribe",
            @params = new { symbols = symbol }
        });

        await ws.SendAsync(
            Encoding.UTF8.GetBytes(sub),
            WebSocketMessageType.Text,
            true,
            ct);

        byte[] buffer = new byte[8192];

        while (ws.State == WebSocketState.Open &&
               !ct.IsCancellationRequested)
        {
            var result = await ws.ReceiveAsync(buffer, ct);

            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("price", out var priceEl))
                continue;

            decimal price = priceEl.GetDecimal();
            string sym = doc.RootElement.GetProperty("symbol").GetString()!;

            yield return new Quote
            {
                Symbol = sym,
                LastTradedPrice = price,
                Bid = price,
                Ask = price,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    public Task<string> GetSymbolFromIsinAsync(string isin, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
