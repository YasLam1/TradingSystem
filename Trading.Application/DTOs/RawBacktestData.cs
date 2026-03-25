using System.Text.Json.Serialization;
using Trading.Domain.Entities;

namespace Trading.Application.DTOs;

public class RawBacktestData
{
    [JsonIgnore]
    public decimal InitialCapital { get; init; }
    [JsonIgnore]
    public List<Execution> Executions { get; init; } = [];
    [JsonIgnore]
    public List<decimal> EquityCurve { get; init; } = [];
    [JsonIgnore]
    public BacktestResult BacktestResult => BacktestResult.From(this);
    public BacktestAnalysis BacktestAnalysis => BacktestAnalysis.Analyze(BacktestResult);
}
