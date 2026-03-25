using Trading.Domain.Entities;
using Trading.Domain.Enums;
using Trading.Domain.Interfaces;

namespace Trading.Simulation;

public class MarketExecutionSimulator : IOrderExecutorSimulator
{
    public Execution Execute(Order order, Quote quote)
    {
        decimal price =
            order.Side == OrderSide.Buy
            ? quote.Ask
            : quote.Bid;

        return new Execution
        {
            OrderId = order.Id,
            Symbol = order.Symbol,
            Side = order.Side,
            FillPrice = price,
            FilledQuantity = order.Quantity,
            Timestamp = quote.Timestamp
        };
    }
}
