using Trading.Domain.Entities;

namespace Trading.Domain.Services;

/// <summary>
/// Choose quantity so that if stop is hit,
/// we lose exactly the _riskPercent we decided.
/// </summary>
public class QuantityCalculator
{
    private readonly decimal _riskPercent;

    public QuantityCalculator(decimal riskPercent)
        => _riskPercent = riskPercent;

    public int Calculate(Account account, decimal entry, decimal stop)
    {
        decimal equity = account.Equity(entry);

        decimal maxLoss = equity * _riskPercent;

        decimal stopDistance = Math.Abs(entry - stop);
        if (stopDistance == 0) return 0;

        decimal rawQty = maxLoss / stopDistance;

        return (int)Math.Floor(rawQty);
    }
}