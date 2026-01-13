using Trading.Domain.Entities;

namespace Trading.Domain.Interfaces;

public interface IMarketDataFeed
{
    IAsyncEnumerable<Quote> StartPriceStreamAsync(string symbol, CancellationToken ct);
}
