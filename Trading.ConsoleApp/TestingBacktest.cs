using Trading.Application.Backtesting;
using Trading.Domain.Risks;
using Trading.Domain.Entities;
using Trading.Domain.Interfaces;
using Trading.Domain.Strategies;
using Trading.Simulation;

public class TestingBacktest
{
    public static void Test()
    {
        const string symbol = "AAPL";
        const decimal initialCapital = 10000;

        Account account = new("BACKTEST", initialCapital);

        IRiskManager riskManager = new RiskManager(account);

        BacktestEngine backtestEngine = new(
            dataFeed: new MockHistoricalDataFeed(),
            strategy: new EmaPullbackRsiAtrStrategy(symbol, riskManager, account),
            execution: new MarketExecutionSimulator()
            );

        var raw = backtestEngine.Run(symbol,
            from: DateTime.Parse("06/02/2012"),
            to: DateTime.Now,
            account: account);

        JsonUtils.Export(raw, nameof(raw));
    }
}