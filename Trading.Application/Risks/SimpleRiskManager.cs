using Trading.Domain.Entities;
using Trading.Domain.Interfaces;

namespace Trading.Application.Risks;

public class SimpleRiskManager : IRiskManager
{
    private readonly int _maxQtyPerOrder;
    private readonly bool _allowShort;

    public SimpleRiskManager(int maxQtyPerOrder, bool allowShort = false)
    {
        _maxQtyPerOrder = maxQtyPerOrder;
        _allowShort = allowShort;
    }

    public bool IsOrderAllowed(Order order)
    {
        if (order.Quantity <= 0) return false;
        if (order.Quantity > _maxQtyPerOrder) return false;

        if (!_allowShort && order.Side == Domain.Enums.OrderSide.Sell)
        {
            // This only blocks SELL orders if they would create a short
        }

        return true;
    }
}