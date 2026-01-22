using Trading.Domain.Entities;
using Trading.Domain.Enums;
using Trading.Domain.Interfaces;

namespace Trading.Application.Risks;

public class SimpleRiskManager : IRiskManager
{
    private readonly Account _account;

    private const int MAX_OPEN_POSITIONS = 5;
    private const decimal MAX_EXPOSURE_PCT = 1m;
    private const decimal MAX_DAILY_LOSS_PCT = 0.03m;

    private readonly bool _allowShort;

    private decimal _dayStartEquity;

    public SimpleRiskManager(Account account, bool allowShort = false)
    {
        _account = account;
        _allowShort = allowShort;
        _dayStartEquity = account.Cash;
    }

    public bool IsOrderAllowed(Order order)
    {
        if (order.Quantity <= 0)
            return false;

        if (!ShortIsAllowed(order))
            return false;

        if (TooManyPositions(order))
            return false;

        if (ExposureTooLarge(order))
            return false;

        if (DailyLossLimitHit(order))
            return false;

        return true;
    }

    private bool ShortIsAllowed(Order order)
    {
        if (_allowShort)
            return true;

        if (order.Side != OrderSide.Sell)
            return true;

        if (!_account.Positions.TryGetValue(order.Symbol, out var pos))
            return false;

        return pos.NetQuantity >= order.Quantity;
    }

    private bool TooManyPositions(Order order)
    {
        if (order.Side != OrderSide.Buy)
            return false;

        return _account.Positions.Count(p => p.Value.NetQuantity != 0) >= MAX_OPEN_POSITIONS;
    }

    private bool ExposureTooLarge(Order order)
    {
        decimal price = order.ReferencePrince.Value;

        decimal equity = _account.Equity(price);

        decimal currentExposure = _account.Positions.Sum(p => Math.Abs(p.Value.NetQuantity * price));

        decimal newExposure = currentExposure + (order.Quantity * price);

        return newExposure > equity * MAX_EXPOSURE_PCT;
    }

    private bool DailyLossLimitHit(Order order)
    {
        decimal equity = _account.Equity(order.ReferencePrince.Value);

        decimal dailyLoss = _dayStartEquity - equity;

        return dailyLoss > _dayStartEquity * MAX_DAILY_LOSS_PCT;
    }

    public void ResetDailyLimits()
        => _dayStartEquity = _account.Cash;

    public Order AdjustOrder(Order order)
    {
        throw new NotImplementedException();
    }
}