using Trading.Domain.Entities;
using Trading.Domain.Enums;
using Trading.Domain.Interfaces;

namespace Trading.Application.Risks;

public class RiskManager : IRiskManager
{
    private readonly Account _account;

    private const int MAX_QTY_PER_ORDER = 1000;
    private const decimal MAX_EXPOSURE_PCT = 0.30m;

    public RiskManager(Account account)
    {
        _account = account;
    }

    public Order AdjustOrder(Order order)
    {
        if (order == null || order.Quantity <= 0)
            return null;

        if (order.ReferencePrice == null || order.ReferencePrice <= 0)
            throw new InvalidOperationException("Order must have ReferencePrince for risk checks.");

        decimal price = order.ReferencePrice.Value;

        int qty = order.Quantity;

        qty = ApplyMaxQty(qty);
        qty = ApplyExposureLimit(qty, order.Symbol, price, order.Side);
        qty = ApplyCashLimit(qty, order.Side, price);

        if (qty <= 0)
            return null;

        order.Quantity = qty;
        return order;
    }

    private int ApplyMaxQty(int qty) => Math.Min(qty, MAX_QTY_PER_ORDER);

    private int ApplyExposureLimit(int qty, string symbol, decimal price, OrderSide side)
    {
        decimal equity = _account.Equity(price);
        decimal maxExposure = equity * MAX_EXPOSURE_PCT;

        decimal currentExposure = 0m;
        if (_account.Positions.TryGetValue(symbol, out var pos))
            currentExposure = Math.Abs(pos.NetQuantity * price);

        decimal newExposure = side == OrderSide.Buy
            ? currentExposure + qty * price
            : currentExposure - qty * price;

        newExposure = Math.Max(0, newExposure);

        if (newExposure <= maxExposure)
            return qty;

        if (side == OrderSide.Sell)
            return qty;

        decimal allowed = maxExposure - currentExposure;
        if (allowed <= 0)
            return 0;

        return (int)(allowed / price);
    }

    private int ApplyCashLimit(int qty, OrderSide side, decimal price)
    {
        if (side == OrderSide.Sell)
            return qty;

        decimal requiredCash = qty * price;
        if (requiredCash <= _account.Cash)
            return qty;

        return (int)(_account.Cash / price);
    }
}