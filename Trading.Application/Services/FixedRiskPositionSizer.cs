using Trading.Domain.Entities;

namespace Trading.Application.Services;

/// <summary>
/// Choose quantity so that if stop is hit,
/// we lose exactly the % we decided.
/// </summary>
public class FixedRiskPositionSizer
{
    private readonly decimal _riskPercent;

    public FixedRiskPositionSizer(decimal riskPercent)
        => _riskPercent = riskPercent;

    public int CalculateQuantity(
        Account account, decimal entry, decimal stop)
    {
        decimal equity = account.Equity(entry);

        decimal maxLoss = equity * _riskPercent;

        decimal stopDistance = Math.Abs(entry - stop);

        if (stopDistance <= 0)
            return 0;

        decimal rawQty = maxLoss / stopDistance;

        return (int)Math.Floor(rawQty);
    }
}
