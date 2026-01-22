using Trading.Domain.Entities;
using Trading.Domain.Enums;
using Trading.Domain.Interfaces;

namespace Trading.Application.Risks;

public class SmartRiskManager : IRiskManager
{
    private readonly Account _account;

    private const int MAX_QTY_PER_ORDER = 1000;
    private const decimal MAX_EXPOSURE_PCT = 0.30m; // 30%

    private readonly bool _allowShort;

    public SmartRiskManager(Account account, bool allowShort = false)
    {
        _account = account;
        _allowShort = allowShort;
    }

    public Order AdjustOrder(Order order)
    {
        if (order.Quantity <= 0)
            return null;

        int qty = order.Quantity;

        qty = ApplyMaxQty(qty);
        qty = ApplyExposureLimit(qty, order);
        qty = ApplyShortRules(qty, order);

        if (qty <= 0)
            return null;

        order.Quantity = qty;
        return order;
    }

    private int ApplyMaxQty(int qty)
        => Math.Min(qty, MAX_QTY_PER_ORDER);

    private int ApplyExposureLimit(int qty, Order order)
    {
        decimal price = order.ReferencePrince.Value;

        decimal equity = _account.Equity(price);

        decimal currentExposure = _account.Positions.Sum(p => Math.Abs(p.Value.NetQuantity * price));

        decimal maxExposure = equity * MAX_EXPOSURE_PCT;

        decimal available = maxExposure - currentExposure;

        if (available <= 0)
            return 0;

        int maxQty = (int)(available / price);

        return Math.Min(qty, maxQty);
    }

    private int ApplyShortRules(int qty, Order order)
    {
        if (_allowShort)
            return qty;

        if (order.Side != OrderSide.Sell)
            return qty;

        if (!_account.Positions.TryGetValue(order.Symbol, out var pos))
            return 0;

        return Math.Min(qty, pos.NetQuantity);
    }

    public bool IsOrderAllowed(Order order)
    {
        throw new NotImplementedException();
    }
}