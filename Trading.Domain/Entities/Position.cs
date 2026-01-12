namespace Trading.Domain.Entities;

public class Position
{
    public string Symbol { get; set; }
    public int Quantity { get; private set; }
    public decimal AveragePrice { get; private set; }

    public Position(string symbol) => Symbol = symbol;

    public void ApplyBuy(int qty, decimal price)
    {
        if (qty <= 0) return;
        int newQty = Quantity + qty;
        AveragePrice = (AveragePrice * Quantity + price * qty) / newQty;
        Quantity = newQty;
    }

    public void ApplySell(int qty)
    {
        if (qty <= 0) return;

        // Ne pas vendre plus que ce qu'on a
        qty = Math.Min(qty, Quantity);
        Quantity -= qty;

        // Si tout vendu, remettre le prix à zéro
        if (Quantity == 0) AveragePrice = 0m;
    }

    public decimal UnrealizedPnl(decimal markPrice)
        => (markPrice - AveragePrice) * Quantity;
}
