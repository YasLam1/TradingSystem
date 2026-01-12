using Trading.Domain.Enums;

namespace Trading.Domain.Entities;

public class Execution
{
    public Guid OrderId { get; set; }
    public string Symbol { get; set; }
    public OrderSide Side { get; set; }
    public decimal FillPrice { get; set; }
    public int FilledQuantity { get; set; }
    public DateTime Timestamp { get; set; }
}