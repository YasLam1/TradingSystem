using Trading.Domain.Enums;

namespace Trading.Domain.Entities;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Symbol { get; set; }
    public OrderSide Side { get; set; }
    public OrderType Type { get; set; }
    public int Quantity { get; set; }
    public decimal? Price { get; set; }
}