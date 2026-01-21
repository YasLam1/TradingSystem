using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Trading.Domain.Entities;
using Trading.Domain.Interfaces;

namespace Trading.Infrastructure.MarketData;

public class AlphaVantageMarketDataFeed : IMarketDataFeed
{
    private readonly string _apiKey;

    public AlphaVantageMarketDataFeed()
        => _apiKey = Environment.GetEnvironmentVariable("AlphaVantage_API_KEY")
                     ?? throw new Exception("API key missing");

    public async IAsyncEnumerable<Quote> StartPriceStreamAsync(
        string symbol, [EnumeratorCancellation] CancellationToken ct)
    {
        using var http = new HttpClient();

        while (!ct.IsCancellationRequested)
        {
            string url =
                $"https://www.alphavantage.co/query" +
                $"?function=GLOBAL_QUOTE" +
                $"&symbol={symbol}" +
                $"&apikey={_apiKey}";

            var json = await http.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("Global Quote", out var quoteEl))
            {
                decimal price =
                    decimal.Parse(
                        quoteEl.GetProperty("05. price").GetString()!,
                        CultureInfo.InvariantCulture);

                yield return new Quote
                {
                    Symbol = symbol,
                    Bid = price,
                    Ask = price,
                    LastTradedPrice = price,
                    Timestamp = DateTime.UtcNow
                };
            }

            // AlphaVantage free tier limit: 5 calls/min
            await Task.Delay(TimeSpan.FromSeconds(15), ct);
        }
    }

    public async Task<string> GetSymbolFromIsinAsync(
        string isin, CancellationToken ct)
    {
        using var http = new HttpClient();

        string url = $"https://query2.finance.yahoo.com/v1/finance/search?q={isin}";

        var json = await http.GetStringAsync(url, ct);
        using var doc = JsonDocument.Parse(json);

        var quotes = doc.RootElement.GetProperty("quotes");

        foreach (var q in quotes.EnumerateArray())
        {
            // ETF / equity results only
            if (q.TryGetProperty("symbol", out var symEl))
            {
                return symEl.GetString();
            }
        }

        return null;
    }

}
