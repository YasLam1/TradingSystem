namespace Trading.Domain.Entities;

public class Position
{
    public string Symbol { get; set; }
    public int Quantity { get; private set; }
    public decimal AveragePrice { get; private set; }

    public Position(string symbol) => Symbol = symbol;

    public void Buy(int quantity, decimal price)
    {
        if (quantity <= 0)
            return;

        // Currently short
        if (Quantity < 0)
        {
            int shortAbs = Math.Abs(Quantity);

            if (quantity < shortAbs)
            {
                // Partial cover
                Quantity += quantity;
                return;
            }
            else
            {
                // Full cover
                quantity -= shortAbs;
                Quantity = 0;
                AveragePrice = 0m;
            }
        }

        // Flat or long
        if (quantity > 0)
        {
            if (Quantity == 0)
            {
                // Open new long
                Quantity = quantity;
                AveragePrice = price;
            }
            else
            {
                // Increase existing long
                decimal totalCost =
                    (AveragePrice * Quantity) +
                    (price * quantity);

                Quantity += quantity;
                AveragePrice = totalCost / Quantity;
            }
        }
    }

    public void Sell(int quantity, decimal price)
    {
        if (quantity <= 0)
            return;

        // Currently long
        if (Quantity > 0)
        {
            if (quantity < Quantity)
            {
                // Partial sell
                Quantity -= quantity;
                return;
            }
            else
            {
                // Full close
                quantity -= Quantity;
                Quantity = 0;
                AveragePrice = 0m;
            }
        }

        // Flat or short
        if (quantity > 0)
        {
            if (Quantity == 0)
            {
                // Open new short
                Quantity = -quantity;
                AveragePrice = price;
            }
            else
            {
                // Increase existing short
                int absQty = Math.Abs(Quantity);

                decimal totalProceeds =
                    (AveragePrice * absQty) +
                    (price * quantity);

                Quantity -= quantity;
                AveragePrice = totalProceeds / Math.Abs(Quantity);
            }
        }
    }

    public decimal UnrealizedPnl(decimal markPrice)
        => (markPrice - AveragePrice) * Quantity;
}
