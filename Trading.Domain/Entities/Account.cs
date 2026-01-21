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
        Position pos = GetOrCreatePosition(exec.Symbol);
        pos.Apply(exec);

        if (exec.Side == OrderSide.Buy)
            Cash -= exec.FilledQuantity * exec.FillPrice;
        else
            Cash += exec.FilledQuantity * exec.FillPrice;
    }

    public decimal UnrealizedPnl(string symbol, decimal markPrice)
    {
        return Positions.TryGetValue(symbol, out Position pos)
            ? pos.UnrealizedPnl(markPrice)
            : 0m;
    }
}
