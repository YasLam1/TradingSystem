using Trading.Domain.Entities;

namespace Trading.Domain.Interfaces;

public interface IOrderExecutorSimulator
{
    Execution Execute(Order order, Quote quote);
}
