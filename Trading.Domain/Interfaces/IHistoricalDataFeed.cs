using Trading.Domain.Entities;

namespace Trading.Domain.Interfaces;

public interface IHistoricalDataFeed
{
    IEnumerable<Quote> GetQuotes(
       string symbol, DateTime from, DateTime to);
}
