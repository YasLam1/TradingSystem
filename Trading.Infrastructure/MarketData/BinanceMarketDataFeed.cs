using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Trading.Domain.Entities;
using Trading.Domain.Interfaces;

namespace Trading.Infrastructure.MarketData;

public class BinanceMarketDataFeed : IMarketDataFeed
{
    const string BaseUrl = "wss://stream.binance.com:9443/ws";

    public async IAsyncEnumerable<Quote> StartPriceStreamAsync(
        string symbol, [EnumeratorCancellation] CancellationToken ct)
    {
        string streamName = $"{symbol.ToLower()}@aggTrade";
        string wsUrl = $"{BaseUrl}/{streamName}";

        using ClientWebSocket ws = new();
        await ws.ConnectAsync(new Uri(wsUrl), ct);

        var buffer = new ArraySegment<byte>(new byte[8192]);

        while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close)
                break;

            var json = Encoding.UTF8.GetString(buffer.Array!, 0, result.Count);
            var msg = JsonDocument.Parse(json);

            if (msg.RootElement.TryGetProperty("p", out var priceElement) &&
                msg.RootElement.TryGetProperty("s", out var symbolElement))
            {
                var priceString = priceElement.GetString()?.Replace(".",",");
                if (decimal.TryParse(priceString, out decimal price))
                {
                    yield return new Quote
                    {
                        Symbol = symbolElement.GetString()!,
                        Bid = price, // Binance aggTrade doesn't include bid/ask
                        Ask = price,
                        LastTradedPrice = price,
                        Timestamp = DateTime.UtcNow
                    };
                }
            }
        }
    }
}