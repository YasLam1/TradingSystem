using System.Globalization;
using Trading.Domain.Entities;
using Trading.Domain.Interfaces;

namespace Trading.Simulation;

public class MockHistoricalDataFeed : IHistoricalDataFeed
{
    public IEnumerable<Quote> GetQuotes(string symbol, DateTime from, DateTime to)
        => Read(@"C:\Users\Yassine Lamrhary\Downloads\AAPL_1min_synthetic_2024_utc.csv", symbol);

    public static IEnumerable<Quote> Read(string filePath, string symbol)
    {
        var lines = File.ReadAllLines(filePath).Skip(1); // skip header

        foreach (string line in lines)
        {
            var parts = line.Split(',');
            DateTime time = DateTime.Parse(parts[0], CultureInfo.InvariantCulture);
            decimal price = decimal.Parse(parts[4], CultureInfo.InvariantCulture);

            yield return new Quote
            {
                Symbol = symbol,
                Timestamp = time,
                Bid = price,
                Ask = price,
                LastTradedPrice = price
            };
        }
    }
}