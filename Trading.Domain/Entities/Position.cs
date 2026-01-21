using Trading.Domain.Enums;

namespace Trading.Domain.Entities;

public class Position
{
    public string Symbol { get; set; }
    public int NetQuantity { get; private set; }
    public decimal AverageEntryPrice { get; private set; }

    public Position(string symbol) => Symbol = symbol;

    public void Buy(int quantity, decimal price)
    {
        if (quantity <= 0)
            return;

        // Currently short
        if (NetQuantity < 0)
        {
            int shortAbs = Math.Abs(NetQuantity);

            if (quantity < shortAbs)
            {
                // Partial cover
                NetQuantity += quantity;
                return;
            }
            else
            {
                // Full cover
                quantity -= shortAbs;
                NetQuantity = 0;
                AverageEntryPrice = 0m;
            }
        }

        // Flat or long
        if (quantity > 0)
        {
            if (NetQuantity == 0)
            {
                // Open new long
                NetQuantity = quantity;
                AverageEntryPrice = price;
            }
            else
            {
                // Increase existing long
                decimal totalCost =
                    (AverageEntryPrice * NetQuantity) +
                    (price * quantity);

                NetQuantity += quantity;
                AverageEntryPrice = totalCost / NetQuantity;
            }
        }
    }

    public void Sell(int quantity, decimal price)
    {
        if (quantity <= 0)
            return;

        // Currently long
        if (NetQuantity > 0)
        {
            if (quantity < NetQuantity)
            {
                // Partial sell
                NetQuantity -= quantity;
                return;
            }
            else
            {
                // Full close
                quantity -= NetQuantity;
                NetQuantity = 0;
                AverageEntryPrice = 0m;
            }
        }

        // Flat or short
        if (quantity > 0)
        {
            if (NetQuantity == 0)
            {
                // Open new short
                NetQuantity = -quantity;
                AverageEntryPrice = price;
            }
            else
            {
                // Increase existing short
                int absQty = Math.Abs(NetQuantity);

                decimal totalProceeds =
                    (AverageEntryPrice * absQty) +
                    (price * quantity);

                NetQuantity -= quantity;
                AverageEntryPrice = totalProceeds / Math.Abs(NetQuantity);
            }
        }
    }

    public void Apply(Execution exec)
    {
        if (exec.Side == OrderSide.Buy)
            Buy(exec.FilledQuantity, exec.FillPrice);
        else
            Sell(exec.FilledQuantity, exec.FillPrice);
    }

    public decimal UnrealizedPnl(decimal markPrice)
        => (markPrice - AverageEntryPrice) * NetQuantity;
}
