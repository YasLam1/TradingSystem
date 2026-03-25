namespace Trading.Application.DTOs;

public class BacktestAnalysis
{
    public string TotalReturnMessage { get; init; }
    public string MaxDrawdownMessage { get; init; }

    public string TradeCountMessage { get; init; }
    public string WinLossMessage { get; init; }

    public string AvgWinLossMessage { get; init; }
    public string SharpeRatioMessage { get; init; }
    public string ProfitFactorMessage { get; init; }
    public string ExpectancyMessage { get; init; }

    public static BacktestAnalysis Analyze(BacktestResult r)
    {
        return new BacktestAnalysis
        {
            // ---- TOTAL RETURN ----
            TotalReturnMessage =
                r.TotalReturnPct > 0
                    ? $"Total Return: {r.TotalReturnPct:F2}% → Strategy made money."
                    : $"Total Return: {r.TotalReturnPct:F2}% → Strategy lost money.",

            // ---- MAX DRAWDOWN ----
            MaxDrawdownMessage =
                r.MaxDrawdownPct > -10
                    ? $"Max Drawdown: {r.MaxDrawdownPct:F2}% → Very safe."
                    : r.MaxDrawdownPct > -20
                        ? $"Max Drawdown: {r.MaxDrawdownPct:F2}% → Acceptable but uncomfortable."
                        : $"Max Drawdown: {r.MaxDrawdownPct:F2}% → Very risky.",

            // ---- TRADE COUNT ----
            TradeCountMessage =
                $"Trades: {r.WinCount + r.LossCount}",

            WinLossMessage =
                $"Wins: {r.WinCount}, Losses: {r.LossCount}",

            // ---- AVG WIN / LOSS ----
            AvgWinLossMessage =
                r.AvgWin > r.AvgLoss
                    ? $"Avg Win ({r.AvgWin:F2}%) > Avg Loss ({r.AvgLoss:F2}%) → Good trade structure."
                    : $"Avg Win ({r.AvgWin:F2}%) <= Avg Loss ({r.AvgLoss:F2}%) → Losing structure.",

            // ---- SHARPE RATIO ----
            SharpeRatioMessage =
                r.SharpeRatio < 0
                    ? $"Sharpe Ratio: {r.SharpeRatio:F2} → Bad."
                    : r.SharpeRatio < 0.5m
                        ? $"Sharpe Ratio: {r.SharpeRatio:F2} → Weak."
                        : r.SharpeRatio < 1m
                            ? $"Sharpe Ratio: {r.SharpeRatio:F2} → Decent."
                            : $"Sharpe Ratio: {r.SharpeRatio:F2} → Very good.",

            // ---- PROFIT FACTOR ----
            ProfitFactorMessage =
                r.ProfitFactor < 1
                    ? $"Profit Factor: {r.ProfitFactor:F2} → Losing strategy."
                    : r.ProfitFactor < 1.3m
                        ? $"Profit Factor: {r.ProfitFactor:F2} → Weak edge."
                        : $"Profit Factor: {r.ProfitFactor:F2} → Good edge.",

            // ---- EXPECTANCY ----
            ExpectancyMessage =
                r.Expectancy <= 0
                    ? $"Expectancy: {r.Expectancy:F4} → No edge per trade."
                    : $"Expectancy: {r.Expectancy:F4} → Positive edge per trade."
        };
    }

}
