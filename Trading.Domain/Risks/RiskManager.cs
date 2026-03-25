using Trading.Domain.Entities;
using Trading.Domain.Enums;
using Trading.Domain.Interfaces;

namespace Trading.Domain.Risks;

public class RiskManager : IRiskManager
{
    private readonly Account _account;
    private readonly bool _allowShort;

    private const int MAX_QTY_PER_ORDER = 1000;
    private const decimal MAX_EXPOSURE_PCT = 0.30m;

    public RiskManager(Account account, bool allowShort = false)
    {
        _account = account;
        _allowShort = allowShort;
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
        qty = ApplyShortRules(qty, order);

        if (qty <= 0)
            return null;

        order.Quantity = qty;
        return order;
    }

    private int ApplyMaxQty(int qty)
        => Math.Min(qty, MAX_QTY_PER_ORDER);

    // Single-symbol exposure model
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
            return qty; // selling always reduces risk

        decimal allowed = maxExposure - currentExposure;
        if (allowed <= 0)
            return 0;

        return (int)(allowed / price);
    }

    private int ApplyCashLimit(int qty, OrderSide side, decimal price)
    {
        if (side == OrderSide.Sell)
            return qty; // selling increases cash

        decimal requiredCash = qty * price;
        if (requiredCash <= _account.Cash)
            return qty;

        return (int)(_account.Cash / price);
    }

    private int ApplyShortRules(int qty, Order order)
    {
        if (_allowShort)
            return qty;

        if (order.Side != OrderSide.Sell)
            return qty;

        if (!_account.Positions.TryGetValue(order.Symbol, out var pos))
            return 0;

        // Prevent naked shorting
        return Math.Min(qty, pos.NetQuantity);
    }
}