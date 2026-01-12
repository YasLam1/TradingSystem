using Trading.Domain.Entities;

namespace Trading.Domain.Interfaces;

public interface IMarketDataFeed
{
    IAsyncEnumerable<Quote> SubscribeAsync(string symbol, CancellationToken ct);
}
