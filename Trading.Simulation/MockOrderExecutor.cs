using Trading.Domain.Entities;
using Trading.Domain.Enums;
using Trading.Domain.Interfaces;

namespace Trading.Simulation;

public sealed class MockOrderExecutor : IOrderExecutor
{
    public Task<Execution> ExecuteOrderAsync(Order order, Quote lastQuote, CancellationToken ct)
    {
        // Choix du prix d'exécution
        decimal execPrice;

        if (order.Type == OrderType.Market)
        {
            execPrice = order.Side == OrderSide.Buy ? lastQuote.Ask : lastQuote.Bid;
        }
        else if (order.Type == OrderType.Limit)
        {
            if (!order.ReferencePrice.HasValue)
                throw new InvalidOperationException("Limit order requires Price");

            decimal limit = order.ReferencePrice.Value;

            bool marketable = order.Side == OrderSide.Buy
                ? lastQuote.Ask <= limit
                : lastQuote.Bid >= limit;

            if (!marketable)
            {
                // Reject
                return Task.FromResult(new Execution
                {
                    OrderId = order.Id,
                    Symbol = order.Symbol,
                    Side = order.Side,
                    FillPrice = 0m,
                    FilledQuantity = 0,
                    Timestamp = DateTime.UtcNow
                });
            }

            execPrice = limit; // Exécute au prix limite si marketable
        }
        else
        {
            throw new NotSupportedException($"Unsupported order type: {order.Type}");
        }

        Execution execution = new()
        {
            OrderId = order.Id,
            Symbol = order.Symbol,
            Side = order.Side,
            FillPrice = execPrice,
            FilledQuantity = order.Quantity,
            Timestamp = DateTime.UtcNow
        };

        return Task.FromResult(execution);
    }
}