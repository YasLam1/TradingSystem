using Trading.Domain.Entities;
namespace Trading.Domain.Interfaces;

public interface IOrderRouter
{
    Task<Execution> SendAsync(Order order, Quote lastQuote, CancellationToken ct);
}