//using Trading.Application.UseCases;

//new MonoStrategyTradingLoop(
//    feed: new Trading.Infrastructure.MarketData.MockMarketDataFeed(),
//    strategy: new Trading.Application.Strategies.AlwaysBuyDipStrategy(symbol: "AAPL", tradeQty: 10),
//    risk: new Trading.Application.Risks.SimpleRiskManager(maxQtyPerOrder: 50),
//    executor: new Trading.Infrastructure.OrderExecutors.MockOrderExecutor(),
//    account: new Trading.Domain.Entities.Account(accountId: "123", initialCash: 200),
//    symbol: "AAPL"
//).RunAsync(CancellationToken.None).GetAwaiter().GetResult();

using Trading.Domain.Interfaces;
using Trading.Infrastructure.MarketData;

string apiKey =
    Environment.GetEnvironmentVariable("FINNHUB_API_KEY")
    ?? throw new Exception("API key missing");

IMarketDataFeed feed = new FinnhubMarketDataFeed(apiKey);
TestingDataFeed.Test(feed, "AAPL").GetAwaiter().GetResult();