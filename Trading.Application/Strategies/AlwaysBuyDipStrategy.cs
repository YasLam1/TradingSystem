using Trading.Domain.Entities;
using Trading.Domain.Enums;
using Trading.Domain.Interfaces;

namespace Trading.Application.Strategies;

/// <summary>
// Strategy:
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
public class AlwaysBuyDipStrategy(
    string symbol,
    int tradeQty,
    IRiskManager riskManager,
    decimal dipThreshold = 0.5m,
    int emaPeriod = 20,
    int cooldownSeconds = 30) : IStrategy
{
    private readonly string _symbol = symbol;
    private readonly int _tradeQty = tradeQty;
    private readonly IRiskManager _riskManager = riskManager;

    private readonly decimal _dipThreshold = dipThreshold;
    private readonly int _emaPeriod = emaPeriod;
    private readonly TimeSpan _cooldown = TimeSpan.FromSeconds(cooldownSeconds);

    private decimal? _prevPrice;
    private decimal? _ema;
    private DateTime _lastTradeTime;

    private decimal? _entryPrice;

    private const decimal _minGain = 1.5m; // +1.5%
    private const decimal _maxLoss = 1m;   // -1%

    public Order DecideActionFromQuote(Quote quote)
    {
        if (quote.Symbol != _symbol)
            return null;

        UpdateEma(quote.LastTradedPrice);

        // Cooldown
        if (DateTime.UtcNow - _lastTradeTime < _cooldown)
            return null;

        // EXIT logic
        if (_entryPrice != null)
        {
            if (ShouldTakeProfit(quote) || ShouldStopLoss(quote))
            {
                Order sell = CreateOrder(OrderSide.Sell);

                if (_riskManager.IsOrderAllowed(sell))
                {
                    _entryPrice = null;
                    _lastTradeTime = DateTime.UtcNow;
                    return sell;
                }
            }
        }

        // ENTRY logic
        if (_prevPrice != null && _ema != null)
        {
            if (IsUptrend(quote) && IsSignificantDip(quote))
            {
                Order buy = CreateOrder(OrderSide.Buy);

                if (_riskManager.IsOrderAllowed(buy))
                {
                    _entryPrice = quote.LastTradedPrice;
                    _lastTradeTime = DateTime.UtcNow;
                    _prevPrice = quote.LastTradedPrice;
                    return buy;
                }
            }
        }

        _prevPrice = quote.LastTradedPrice;
        return null;
    }

    private void UpdateEma(decimal price)
    {
        if (_ema == null)
        {
            _ema = price;
            return;
        }

        decimal k = 2m / (_emaPeriod + 1);
        _ema = price * k + _ema * (1 - k);
    }

    private bool IsUptrend(Quote quote)
        => quote.LastTradedPrice > _ema;

    private bool IsSignificantDip(Quote quote)
    {
        decimal drop =
            (_prevPrice.Value - quote.LastTradedPrice) / _prevPrice.Value * 100;

        return drop >= _dipThreshold;
    }

    private bool ShouldTakeProfit(Quote quote)
    {
        decimal gain =
            (quote.LastTradedPrice - _entryPrice.Value) / _entryPrice.Value * 100;

        return gain >= _minGain;
    }

    private bool ShouldStopLoss(Quote quote)
    {
        decimal loss =
            (_entryPrice.Value - quote.LastTradedPrice) / _entryPrice.Value * 100;

        return loss >= _maxLoss;
    }

    private Order CreateOrder(OrderSide side)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            Symbol = _symbol,
            Side = side,
            Type = OrderType.Market,
            Quantity = _tradeQty
        };
    }
}