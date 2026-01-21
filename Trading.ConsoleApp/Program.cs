//using Trading.Application.UseCases;

//new MonoStrategyTradingLoop(
//    feed: new Trading.Infrastructure.MarketData.MockMarketDataFeed(),
//    strategy: new Trading.Application.Strategies.AlwaysBuyDipStrategy(symbol: "AAPL", tradeQty: 10),
//    risk: new Trading.Application.Risks.SimpleRiskManager(maxQtyPerOrder: 50),
//    executor: new Trading.Infrastructure.OrderExecutors.MockOrderExecutor(),
//    account: new Trading.Domain.Entities.Account(accountId: "123", initialCash: 200),
//    symbol: "AAPL"
//).RunAsync(CancellationToken.None).GetAwaiter().GetResult();

TestingBacktest.Test();