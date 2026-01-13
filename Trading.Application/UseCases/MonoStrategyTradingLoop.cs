using Trading.Domain.Entities;
using Trading.Domain.Interfaces;

namespace Trading.Application.UseCases;

public class MonoStrategyTradingLoop(IMarketDataFeed feed, IStrategy strategy, IRiskManager risk, 
    IOrderExecutor router, Account account, string symbol)
{
    private readonly IMarketDataFeed _feed = feed;
    private readonly IStrategy _strategy = strategy;
    private readonly IRiskManager _risk = risk;
    private readonly IOrderExecutor _router = router;
    private readonly Account _account = account;
    private readonly string _symbol = symbol;

    public async Task RunAsync(CancellationToken ct)
    {
        await foreach (Quote? quote in _feed.StartPriceStreamAsync(_symbol, ct))
        {
            Order? order = _strategy.DecideActionFromQuote(quote);
            if (order is null) continue;

            if (!_risk.IsOrderAllowed(order)) continue;

            Execution? exec = await _router.ExecuteOrderAsync(order, quote, ct);
            _account.UpdateAccountWithExecution(exec);

            _account.Positions.TryGetValue(_symbol, out Position? pos);
            decimal uPnL = _account.UnrealizedPnl(_symbol, quote.LastTradedPrice);

            Console.WriteLine($"[{quote.Timestamp:HH:mm:ss.fff}] Exec {exec.Side} {exec.FilledQuantity}@{exec.FillPrice:F4} | Cash={_account.Cash:F2} | Qty={(pos?.NetQuantity ?? 0)} Avg={(pos?.AverageEntryPrice ?? 0):F4} | U-PnL={uPnL:F2}");
        }
    }
}
