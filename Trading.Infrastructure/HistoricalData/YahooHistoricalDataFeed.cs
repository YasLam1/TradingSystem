using Trading.Domain.Entities;
using Trading.Domain.Interfaces;
using System.Text.Json;

namespace Trading.Infrastructure.HistoricalData;

public class YahooHistoricalDataFeed : IHistoricalDataFeed
{
    private readonly HttpClient _httpClient;

    public YahooHistoricalDataFeed()
    {
        _httpClient = new HttpClient();

        _httpClient.DefaultRequestHeaders.Add(
           "User-Agent",
           "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

        _httpClient.DefaultRequestHeaders.Add(
            "Accept",
            "application/json");
    }

    public IEnumerable<Quote> GetQuotes(string symbol, DateTime from, DateTime to)
    {
        string csvFallbackPath = Path.Combine(Path.GetTempPath(), $"{symbol}.csv");
        try
        {
            var quotes = FetchFromYahoo(symbol, from, to);
            CsvQuoteUtils.Save(quotes, csvFallbackPath);
            return quotes;
        }
        catch
        {
            if (!File.Exists(csvFallbackPath))
                csvFallbackPath = @"C:\Users\Yassine Lamrhary\Downloads\fake_price_data.csv";
                //return [];

            return CsvQuoteUtils.Read(csvFallbackPath, symbol)
                .Where(q => q.Timestamp >= from && q.Timestamp <= to);
        }
    }

    public IEnumerable<Quote> FetchFromYahoo(string symbol, DateTime from, DateTime to)
    {
        long period1 = new DateTimeOffset(from).ToUnixTimeSeconds();
        long period2 = new DateTimeOffset(to).ToUnixTimeSeconds();

        string url =
            $"https://query2.finance.yahoo.com/v8/finance/chart/{symbol}"
            + $"?period1={period1}&period2={period2}&interval=1d";

        using var response = _httpClient.GetAsync(url).Result;
        response.EnsureSuccessStatusCode();

        string json = response.Content.ReadAsStringAsync().Result;

        using JsonDocument doc = JsonDocument.Parse(json);
        var root = doc.RootElement.GetProperty("chart").GetProperty("result")[0];

        var timestamps = root.GetProperty("timestamp").EnumerateArray();
        var quoteData = root.GetProperty("indicators").GetProperty("quote")[0];

        var closes = quoteData.GetProperty("close").EnumerateArray();

        var timeEnum = timestamps.GetEnumerator();
        var closeEnum = closes.GetEnumerator();

        while (timeEnum.MoveNext() && closeEnum.MoveNext())
        {
            long unixTime = timeEnum.Current.GetInt64();
            decimal? closePrice = closeEnum.Current.GetDecimal();

            if (closePrice.HasValue)
            {
                yield return new Quote
                {
                    Timestamp = DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime,
                    Bid = closePrice.Value,
                    Ask = closePrice.Value,
                    LastTradedPrice = closePrice.Value,
                    Symbol = symbol
                };
            }
        }
    }
}
