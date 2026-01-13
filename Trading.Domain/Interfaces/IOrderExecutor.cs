using Trading.Domain.Entities;

namespace Trading.Domain.Interfaces;

public interface IOrderExecutor
{
    Task<Execution> ExecuteOrderAsync(Order order, Quote lastQuote, CancellationToken ct);
}