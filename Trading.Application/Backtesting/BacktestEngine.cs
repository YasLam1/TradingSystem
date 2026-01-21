using Trading.Application.DTOs;
using Trading.Domain.Entities;
using Trading.Domain.Enums;
using Trading.Domain.Interfaces;

namespace Trading.Application.Backtesting;

public class BacktestEngine(
    IHistoricalDataFeed dataFeed,
    IStrategy strategy,
    IOrderExecutorSimulator execution)
{
    private readonly IHistoricalDataFeed _dataFeed = dataFeed;
    private readonly IStrategy _strategy = strategy;
    private readonly IOrderExecutorSimulator _execution = execution;

    public RawBacktestData Run(string symbol, DateTime from, DateTime to, decimal initialCapital)
    {
        Position position = new(symbol);
        List<Execution> executions = [];
        List<decimal> equityCurve = [];

        decimal cash = initialCapital;

        foreach (Quote quote in _dataFeed.GetQuotes(symbol, from, to))
        {
            Order order = _strategy.DecideActionFromQuote(quote);
            if (order == null) continue;

            Execution exec = _execution.Execute(order, quote);

            // apply
            position.Apply(exec);
            executions.Add(exec);

            // cash update
            decimal notional = exec.FillPrice * exec.FilledQuantity;
            if (exec.Side == OrderSide.Buy) cash -= notional;
            else cash += notional;

            // equity snapshot
            decimal equity = cash + (position.NetQuantity * quote.Bid);
            equityCurve.Add(equity);
        }

        return new RawBacktestData
        {
            InitialCapital = initialCapital,
            Executions = executions,
            EquityCurve = equityCurve
        };
    }
}
