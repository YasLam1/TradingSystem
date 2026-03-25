using Trading.Domain.Entities;
using Trading.Domain.Enums;

namespace Trading.Application.DTOs;

public class BacktestResult
{
    public decimal TotalReturnPct { get; init; }
    public decimal MaxDrawdownPct { get; init; }
    public decimal SharpeRatio { get; init; }
    public decimal ProfitFactor { get; init; }
    public decimal Expectancy { get; init; }

    public int WinCount { get; init; }
    public int LossCount { get; init; }
    public decimal WinRate => WinCount / (WinCount + LossCount);

    public decimal AvgWin { get; init; }
    public decimal AvgLoss { get; init; }

    public static BacktestResult From(RawBacktestData raw)
    {
        var (winCount, lossCount, avgWin, avgLoss) =
            AnalyzeExecutions(raw.Executions);

        return new BacktestResult
        {
            TotalReturnPct = ComputeTotalReturn(raw),
            MaxDrawdownPct = ComputeMaxDrawdown(raw.EquityCurve),
            SharpeRatio = ComputeSharpeRatio(raw.EquityCurve),
            ProfitFactor = ComputeProfitFactor(raw.Executions),
            Expectancy = ComputeExpectancy(winCount, lossCount, avgWin, avgLoss),

            WinCount = winCount,
            LossCount = lossCount,
            AvgWin = avgWin,
            AvgLoss = avgLoss
        };
    }

    private static decimal ComputeTotalReturn(RawBacktestData raw)
    {
        if (raw.EquityCurve.Count == 0)
            return 0m;

        decimal finalEquity = raw.EquityCurve.Last();

        return (finalEquity - raw.InitialCapital) / raw.InitialCapital * 100m;
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
                decimal dd = (value - peak) / peak * 100m;

                if (dd < maxDd)
                    maxDd = dd;
            }
        }

        return maxDd;
    }

    private static (int winCount, int lossCount, decimal avgWin, decimal avgLoss)
        AnalyzeExecutions(List<Execution> executions)
    {
        int winCount = 0;
        int lossCount = 0;

        decimal totalWinPct = 0m;
        decimal totalLossPct = 0m;

        Execution lastBuy = null;

        foreach (Execution exec in executions.OrderBy(e => e.Timestamp))
        {
            if (exec.Side == OrderSide.Buy)
            {
                lastBuy = exec;
            }
            else if (exec.Side == OrderSide.Sell && lastBuy != null)
            {
                decimal pnlPct =
                    (exec.FillPrice - lastBuy.FillPrice)
                    / lastBuy.FillPrice * 100m;

                if (pnlPct > 0)
                {
                    winCount++;
                    totalWinPct += pnlPct;
                }
                else if (pnlPct < 0)
                {
                    lossCount++;
                    totalLossPct += Math.Abs(pnlPct);
                }

                lastBuy = null;
            }
        }

        decimal avgWin = winCount > 0 ? totalWinPct / winCount : 0m;
        decimal avgLoss = lossCount > 0 ? totalLossPct / lossCount : 0m;

        return (winCount, lossCount, avgWin, avgLoss);
    }

    public static decimal ComputeSharpeRatio(List<decimal> equity)
    {
        if (equity.Count < 2)
            return 0m;

        List<decimal> returns = [];

        for (int i = 1; i < equity.Count; i++)
        {
            decimal r = (equity[i] - equity[i - 1]) / equity[i - 1];
            returns.Add(r);
        }

        decimal avgReturn = returns.Average();

        decimal variance = returns
            .Select(r => (r - avgReturn) * (r - avgReturn))
            .Average();

        decimal stdDev = (decimal)Math.Sqrt((double)variance);
        if (stdDev == 0) return 0m;

        return avgReturn / stdDev;
    }
    
    public static decimal ComputeProfitFactor(List<Execution> executions)
    {
        decimal totalWin = 0m;
        decimal totalLoss = 0m;

        Execution lastBuy = null;

        foreach (var exec in executions.OrderBy(e => e.Timestamp))
        {
            if (exec.Side == OrderSide.Buy)
            {
                lastBuy = exec;
            }
            else if (exec.Side == OrderSide.Sell && lastBuy != null)
            {
                decimal pnl = exec.FillPrice - lastBuy.FillPrice;

                if (pnl > 0)
                    totalWin += pnl;
                else
                    totalLoss += Math.Abs(pnl);

                lastBuy = null;
            }
        }

        if (totalLoss == 0)
            return 0m;

        return totalWin / totalLoss;
    }
    
    public static decimal ComputeExpectancy(
        int winCount, int lossCount, decimal avgWin, decimal avgLoss)
    {
        int total = winCount + lossCount;
        if (total == 0) return 0m;

        decimal winRate = (decimal)winCount / total;
        decimal lossRate = (decimal)lossCount / total;

        return winRate * avgWin - lossRate * avgLoss;
    }
}

