using Trading.Domain.Entities;
namespace Trading.Domain.Interfaces;

public interface IStrategy
{
    Order OnQuote(Quote quote);
}