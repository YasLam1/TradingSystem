using Trading.Domain.Entities;
using Trading.Domain.Enums;
using Trading.Domain.Interfaces;

namespace Trading.Application.Strategies;

/// <summary>
// Strategy explanation:
// - Tracks price updates and maintains an EMA to detect market trend.
// - Enforces a cooldown period to prevent excessive trading.
// - If a position is open:
//     * Sells when profit target is reached.
//     * Sells when stop-loss threshold is hit.
//     * All sell orders are validated by the risk manager.
// - If no position is open:
//     * Buys only when price is above EMA (uptrend).
//     * Buys only after a significant price dip.
//     * All buy orders are validated by the risk manager.
/// </summary>
public class EmaDipStrategy(
    string symbol,
    int quantity,
    IRiskManager riskManager,
    decimal dipThreshold = 0.02m,
    decimal takeProfit = 0.03m,
    decimal stopLoss = 0.01m,
    int emaPeriod = 20,
    int cooldownSeconds = 60) : IStrategy
{
    private readonly string _symbol = symbol;
    private readonly int _qty = quantity;
    private readonly IRiskManager _risk = riskManager;

    private readonly decimal _dipThreshold = dipThreshold;
    private readonly decimal _takeProfit = takeProfit;
    private readonly decimal _stopLoss = stopLoss;

    private readonly int _emaPeriod = emaPeriod;
    private readonly TimeSpan _cooldown = TimeSpan.FromSeconds(cooldownSeconds);

    private decimal? _ema;
    private int _emaCount;

    private decimal? _lastPrice;
    private DateTime _lastTradeTime;

    private decimal? _entryPrice;

    public Order DecideActionFromQuote(Quote q)
    {
        if (q.Symbol != _symbol)
            return null;

        UpdateEma(q.LastTradedPrice);

        if (_emaCount < _emaPeriod)
        {
            _lastPrice = q.LastTradedPrice;
            return null; // warm-up
        }

        // cooldown
        if (_lastTradeTime != default &&
            q.Timestamp - _lastTradeTime < _cooldown)
            return null;

        // EXIT
        if (_entryPrice != null)
        {
            if (HitTakeProfit(q) || HitStopLoss(q))
            {
                var sell = CreateOrder(OrderSide.Sell);

                if (_risk.IsOrderAllowed(sell))
                {
                    _entryPrice = null;
                    _lastTradeTime = q.Timestamp;
                    return sell;
                }
            }
        }

        // ENTRY
        if (_entryPrice == null && _lastPrice != null && 
            IsUptrend(q) && IsDip(q))
        {
            var buy = CreateOrder(OrderSide.Buy);

            if (_risk.IsOrderAllowed(buy))
            {
                _entryPrice = q.LastTradedPrice;
                _lastTradeTime = q.Timestamp;
                _lastPrice = q.LastTradedPrice;
                return buy;
            }
        }

        _lastPrice = q.LastTradedPrice;
        return null;
    }

    private void UpdateEma(decimal price)
    {
        if (_ema == null)
        {
            _ema = price;
            _emaCount = 1;
            return;
        }

        decimal k = 2m / (_emaPeriod + 1);
        _ema = price * k + _ema * (1 - k);
        _emaCount++;
    }

    private bool IsUptrend(Quote q)
        => q.LastTradedPrice > _ema;

    private bool IsDip(Quote q)
    {
        decimal drop = (_lastPrice.Value - q.LastTradedPrice) / _lastPrice.Value;
        return drop >= _dipThreshold;
    }

    private bool HitTakeProfit(Quote q)
    {
        decimal gain = (q.LastTradedPrice - _entryPrice.Value) / _entryPrice.Value;
        return gain >= _takeProfit;
    }

    private bool HitStopLoss(Quote q)
    {
        decimal loss = (_entryPrice.Value - q.LastTradedPrice) / _entryPrice.Value;
        return loss >= _stopLoss;
    }

    private Order CreateOrder(OrderSide side)
        => new()
        {
            Id = Guid.NewGuid(),
            Symbol = _symbol,
            Side = side,
            Type = OrderType.Market,
            Quantity = _qty
        };
}
