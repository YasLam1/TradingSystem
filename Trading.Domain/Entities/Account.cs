using Trading.Domain.Enums;

namespace Trading.Domain.Entities;

public class Account
{
    public string AccountId { get; }
    public decimal Cash { get; private set; }
    public Dictionary<string, Position> Positions { get; } = [];

    public Account(string accountId, decimal initialCash)
    {
        AccountId = accountId;
        Cash = initialCash;
    }

    public Position GetOrCreatePosition(string symbol)
    {
        if (!Positions.TryGetValue(symbol, out Position pos))
        {
            pos = new Position(symbol);
            Positions[symbol] = pos;
        }
        return pos;
    }

    public void UpdateAccountWithExecution(Execution exec)
    {
        var pos = GetOrCreatePosition(exec.Symbol);

        if (exec.Side == OrderSide.Buy)
        {
            // Buying: either increase long or cover short
            // Cash goes down by price * qty
            pos.Buy(exec.FilledQuantity, exec.FillPrice);
            Cash -= exec.FilledQuantity * exec.FillPrice;
        }
        else
        {
            // Selling: either increase short or reduce long
            // Cash goes up by price * qty
            pos.Sell(exec.FilledQuantity, exec.FillPrice);
            Cash += exec.FilledQuantity * exec.FillPrice;
        }
    }

    public decimal UnrealizedPnl(string symbol, decimal markPrice)
    {
        return Positions.TryGetValue(symbol, out var pos)
            ? pos.UnrealizedPnl(markPrice)
            : 0m;
    }
}
