using Trading.Domain.Entities;
using Trading.Domain.Interfaces;
using System.Globalization;
using System.Text.Json;

namespace Trading.Infrastructure.HistoricalData;

public class AlphaVantageHistoricalDataFeed : IHistoricalDataFeed
{
    private readonly string _apiKey;
    private readonly bool _includeExtendedHours = false;
    private readonly HttpClient _http = new();

    private const string BaseUrl = "https://www.alphavantage.co/query";
    private const string _interval = "15min";              // "1min","5min","15min","30min","60min"
    private const int _minDelayBetweenCallsMs = 1000;
    private const int _maxRetries = 2;

    public AlphaVantageHistoricalDataFeed()
    {
        _apiKey = Environment.GetEnvironmentVariable("AlphaVantage_API_KEY") 
            ?? throw new Exception("API key missing");
    }

    public IEnumerable<Quote> GetQuotes(string symbol, DateTime from, DateTime to)
    {
        var results = new List<Quote>();

        // Heuristic: if 'to' is within ~35 days of 'now', one "outputsize=full" call is enough (~30 trailing days).
        var recentWindow = DateTime.UtcNow.AddDays(-35);
        if (to.ToUniversalTime() >= recentWindow)
        {
            var quotes = FetchOneWindowRecent(symbol, from, to);
            results.AddRange(quotes);
        }
        else
        {
            // Deep history: loop month=YYYY-MM slices (Alpha Vantage supports months back to 2000-01).
            foreach (var month in EnumerateMonths(from, to))
            {
                var quotes = FetchOneMonthSlice(symbol, month, from, to);
                results.AddRange(quotes);
            }
        }

        // Ensure sorted ascending by time and unique timestamps
        return results
            .Where(q => q.Timestamp >= from && q.Timestamp <= to)
            .OrderBy(q => q.Timestamp)
            .ThenBy(q => q.Symbol)
            .ToList();
    }

    // --- Internals ---

    private IEnumerable<Quote> FetchOneWindowRecent(string symbol, DateTime from, DateTime to)
    {
        // function=TIME_SERIES_INTRADAY&symbol=...&interval=...&outputsize=full
        var url = $"{BaseUrl}?function=TIME_SERIES_INTRADAY&symbol={Uri.EscapeDataString(symbol)}" +
                  $"&interval={Uri.EscapeDataString(_interval)}" +
                  $"&outputsize=full" +
                  $"&extended_hours={(_includeExtendedHours ? "true" : "false")}" +
                  $"&apikey={Uri.EscapeDataString(_apiKey)}";

        var doc = GetWithRetry(url);
        return ParseQuotesFromIntradayJson(doc, symbol, from, to);
    }

    private IEnumerable<Quote> FetchOneMonthSlice(string symbol, string monthYYYYMM, DateTime from, DateTime to)
    {
        // function=TIME_SERIES_INTRADAY&symbol=...&interval=...&month=YYYY-MM
        var url = $"{BaseUrl}?function=TIME_SERIES_INTRADAY&symbol={Uri.EscapeDataString(symbol)}" +
                  $"&interval={Uri.EscapeDataString(_interval)}" +
                  $"&month={Uri.EscapeDataString(monthYYYYMM)}" +
                  $"&extended_hours={(_includeExtendedHours ? "true" : "false")}" +
                  $"&apikey={Uri.EscapeDataString(_apiKey)}";

        var doc = GetWithRetry(url);
        return ParseQuotesFromIntradayJson(doc, symbol, from, to);
    }

    private JsonDocument GetWithRetry(string url)
    {
        Exception last = null;
        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                if (attempt > 0) Thread.Sleep(_minDelayBetweenCallsMs * (attempt + 1)); // backoff

                // Throttle between consecutive calls
                Thread.Sleep(_minDelayBetweenCallsMs);

                var json = _http.GetStringAsync(url).GetAwaiter().GetResult();
                var doc = JsonDocument.Parse(json);

                // Alpha Vantage returns errors/throttle as "Note" or "Error Message"
                if (doc.RootElement.TryGetProperty("Note", out _) ||
                    doc.RootElement.TryGetProperty("Information", out _) ||
                    doc.RootElement.TryGetProperty("Error Message", out _))
                {
                    // simple backoff and retry
                    last = new InvalidOperationException("Alpha Vantage throttled or returned an error payload.");
                    continue;
                }

                return doc;
            }
            catch (Exception ex)
            {
                last = ex;
            }
        }

        throw last ?? new InvalidOperationException("Alpha Vantage request failed.");
    }

    private IEnumerable<Quote> ParseQuotesFromIntradayJson(JsonDocument doc, string symbol, DateTime from, DateTime to)
    {
        var quotes = new List<Quote>();

        // Find the time-series object: key like "Time Series (5min)" / "(1min)" etc.
        JsonElement root = doc.RootElement;

        JsonElement timeSeries = default;
        bool found = false;
        foreach (var prop in root.EnumerateObject())
        {
            if (prop.Name.StartsWith("Time Series (", StringComparison.OrdinalIgnoreCase))
            {
                timeSeries = prop.Value;
                found = true;
                break;
            }
        }

        if (!found || timeSeries.ValueKind != JsonValueKind.Object)
            return quotes; // nothing to parse (could also throw)

        // Iterate each timestamp entry
        foreach (var entry in timeSeries.EnumerateObject())
        {
            var tsText = entry.Name; // e.g., "2026-01-16 19:55:00"
            if (!DateTime.TryParse(tsText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var ts))
                continue;

            if (ts < from || ts > to) continue;

            var bar = entry.Value;
            if (bar.ValueKind != JsonValueKind.Object) continue;

            // "4. close" is last traded price for the bar
            if (!bar.TryGetProperty("4. close", out var closeEl)) continue;

            if (!decimal.TryParse(closeEl.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var close))
                continue;

            // Bars have no bid/ask; set to 0 (or change Quote to nullable if you prefer).
            quotes.Add(new Quote
            {
                Symbol = symbol,
                Bid = 0m,
                Ask = 0m,
                LastTradedPrice = close,
                Timestamp = ts
            });
        }

        return quotes;
    }

    private static IEnumerable<string> EnumerateMonths(DateTime from, DateTime to)
    {
        // Yields "YYYY-MM" for each month overlapping [from, to]
        var cur = new DateTime(from.Year, from.Month, 1);
        var end = new DateTime(to.Year, to.Month, 1);
        while (cur <= end)
        {
            yield return $"{cur:yyyy-MM}";
            cur = cur.AddMonths(1);
        }
    }
}

