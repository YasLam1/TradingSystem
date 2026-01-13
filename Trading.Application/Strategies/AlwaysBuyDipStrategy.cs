using Trading.Domain.Entities;
using Trading.Domain.Enums;
using Trading.Domain.Interfaces;

namespace Trading.Application.Strategies;

public class AlwaysBuyDipStrategy : IStrategy
{
    private readonly string _symbol;
    private readonly int _tradeQty;
    private decimal? _prev;

    public AlwaysBuyDipStrategy(string symbol, int tradeQty)
    {
        _symbol = symbol;
        _tradeQty = tradeQty;
    }

    public Order? DecideActionFromQuote(Quote quote)
    {
        if (quote.Symbol != _symbol) return null;

        Order? order = null;

        if (_prev is not null)
        {
            // Buy : if price dipped vs last tick
            if (quote.LastTradedPrice < _prev.Value)
            {
                order = new Order
                {
                    Id = Guid.NewGuid(),
                    Symbol = _symbol,
                    Side = OrderSide.Buy,
                    Type = OrderType.Market,
                    Quantity = _tradeQty,
                    Price = null
                };
            }
        }

        _prev = quote.LastTradedPrice;
        return order;
    }
}
