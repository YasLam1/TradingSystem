using Trading.Domain.Entities;
namespace Trading.Domain.Interfaces;

public interface IRiskManager
{
    bool Approve(Order order);
}