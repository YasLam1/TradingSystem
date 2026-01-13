namespace Trading.Domain.Entities;

public class Quote
{
    public string Symbol { get; set; }
    public decimal Bid { get; set; }
    public decimal Ask { get; set; }
    public decimal LastTradedPrice { get; set; }
    public DateTime Timestamp { get; set; }
}