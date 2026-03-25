using Trading.Domain.Entities;
using Trading.Domain.Interfaces;

namespace Trading.Simulation;

public class MockMarketDataFeed : IMarketDataFeed
{
    public async IAsyncEnumerable<Quote> StartPriceStreamAsync(string symbol, CancellationToken ct)
    {
        Random rnd = new();
        decimal last = 100m;
        const decimal spread = 0.02m;

        while (!ct.IsCancellationRequested)
        {
            // small random move
            last += (decimal)((rnd.NextDouble() - 0.5) * 0.2);

            decimal bid = last - spread / 2m;
            decimal ask = last + spread / 2m;

            yield return new Quote
            {
                Symbol = symbol,
                Bid = bid,
                Ask = ask,
                LastTradedPrice = last,
                Timestamp = DateTime.UtcNow
            };

            await Task.Delay(50, ct);
        }
    }

    public Task<string> GetSymbolFromIsinAsync(string isin, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}