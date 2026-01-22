using Trading.Application.DTOs;
using Trading.Domain.Entities;
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

    public RawBacktestData Run(string symbol, DateTime from, DateTime to, Account account)
    {
        decimal initialCapital = account.Cash;

        List<Execution> executions = [];
        List<decimal> equityCurve = [];

        foreach (Quote quote in _dataFeed.GetQuotes(symbol, from, to))
        {
            Order order = _strategy.DecideActionFromQuote(quote);

            if (order != null)
            {
                Execution exec = _execution.Execute(order, quote);
                account.UpdateAccountWithExecution(exec);
                executions.Add(exec);
            }

            // Always mark-to-market
            decimal equity = account.Equity(quote.LastTradedPrice);
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
