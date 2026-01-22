using Trading.Domain.Entities;
using Trading.Domain.Interfaces;

namespace Trading.Infrastructure.HistoricalData;

public class MockHistoricalDataFeed : IHistoricalDataFeed
{
    public IEnumerable<Quote> GetQuotes(string symbol, DateTime from, DateTime to)
        => CsvQuoteUtils.ReadV2(@"C:\Users\yassine.lamrhary\Downloads\AAPL_1min_synthetic_2024_utc.csv", symbol);
}