using Trading.Domain.Entities;

namespace Trading.Application.DTOs;

public class RawBacktestData
{
    public decimal InitialCapital { get; init; }
    public List<Execution> Executions { get; init; } = new();
    public List<decimal> EquityCurve { get; init; } = new();
}
