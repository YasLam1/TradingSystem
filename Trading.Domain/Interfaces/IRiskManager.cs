using Trading.Domain.Entities;
namespace Trading.Domain.Interfaces;

public interface IRiskManager
{
    Order AdjustOrder(Order order);
}