using System.Text.Json.Serialization;
using Trading.Domain.Entities;

namespace Trading.Application.DTOs;

public class RawBacktestData
{
    public decimal InitialCapital { get; init; }
    public List<Execution> Executions { get; init; } = [];
    [JsonIgnore]
    public List<decimal> EquityCurve { get; init; } = [];
    public BacktestResult BacktestResult => BacktestResult.From(this);
}
