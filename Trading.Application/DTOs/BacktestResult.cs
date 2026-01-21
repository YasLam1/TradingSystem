namespace Trading.Application.DTOs;

public class BacktestResult
{
    public decimal TotalReturnPct { get; init; }
    public decimal MaxDrawdownPct { get; init; }
    public int ExecutionCount { get; init; }

    public static BacktestResult From(RawBacktestData raw)
    {
        return new BacktestResult
        {
            TotalReturnPct = ComputeTotalReturn(raw),
            MaxDrawdownPct = ComputeMaxDrawdown(raw.EquityCurve),
            ExecutionCount = raw.Executions.Count
        };
    }

    private static decimal ComputeTotalReturn(RawBacktestData raw)
    {
        if (raw.EquityCurve.Count == 0)
            return 0m;

        decimal finalEquity = raw.EquityCurve.Last();

        return (finalEquity - raw.InitialCapital) / raw.InitialCapital;
    }

    private static decimal ComputeMaxDrawdown(List<decimal> equity)
    {
        if (equity.Count == 0)
            return 0m;

        decimal peak = equity[0];
        decimal maxDd = 0m;

        foreach (decimal value in equity)
        {
            if (value > peak)
                peak = value;

            if (peak > 0m)
            {
                decimal dd = (value - peak) / peak;

                if (dd < maxDd)
                    maxDd = dd;
            }
        }

        return maxDd;
    }
}

