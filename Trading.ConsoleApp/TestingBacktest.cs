using Trading.Application.Backtesting;
using Trading.Application.DTOs;
using Trading.Application.Risks;
using Trading.Application.Strategies;
using Trading.Infrastructure.HistoricalData;
using Trading.Infrastructure.OrderExecutors;

public class TestingBacktest
{
    public static void Test()
    {
        const string symbol = "AAPL";
        const int tradeQte = 10;
        const int maxQtePerOrder = 100;

        BacktestEngine backtestEngine = new(
            dataFeed: new YahooHistoricalDataFeed(),
            strategy: new EmaDipStrategy(symbol, tradeQte, new SimpleRiskManager(maxQtePerOrder)),
            execution: new MarketExecutionSimulator()
            );

        var raw = backtestEngine.Run(symbol,
            from: DateTime.Parse("06/02/2020"),
            to: DateTime.Now,
            initialCapital: 10_000m);

        var result = BacktestResult.From(raw);

        JsonUtils.Export(raw, nameof(raw));
        JsonUtils.Export(result, nameof(result));
    }
}