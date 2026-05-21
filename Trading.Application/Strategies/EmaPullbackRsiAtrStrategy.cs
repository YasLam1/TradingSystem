using Trading.Domain.Entities;
using Trading.Domain.Enums;
using Trading.Domain.Interfaces;
using Trading.Application.Services;

namespace Trading.Application.Strategies;

public class EmaPullbackRsiAtrStrategy : IStrategy
{
    #region Properties
    // EMA
    private const int EMA_FAST_PERIOD = 20;
    private const int EMA_SLOW_PERIOD = 50;
    private decimal? _emaFast;
    private decimal? _emaSlow;

    // RSI
    private const int RSI_PERIOD = 14;
    private const decimal RSI_MIN_LEVEL = 52m;
    private decimal? _averageGain;
    private decimal? _averageLoss;
    private decimal? _previousClose;
    private decimal? _currentRsi;
    private decimal? _previousRsi;

    // ATR
    private const int ATR_PERIOD = 14;
    private const decimal PULLBACK_ATR_BAND = 0.35m;
    private const decimal STOP_ATR_MULTIPLIER = 2.2m;
    private const decimal RISK_REWARD_RATIO = 2.5m;
    private decimal? _atr;
    private Bar _previousBar;

    // Position
    private readonly string _symbol;
    private bool _inPosition;
    private decimal? _stopPrice;
    private decimal? _targetPrice;

    private readonly BarBuilder _barBuilder = new(TimeSpan.FromMinutes(15));
    #endregion

    public EmaPullbackRsiAtrStrategy(string symbol)
    {
        _symbol = symbol;
    }

    public Order DecideActionFromQuote(Quote quote)
    {
        Bar bar = _barBuilder.ProcessQuote(quote);
        if (bar == null) return null;
        return DecideActionFromBar(bar);
    }

    private Order DecideActionFromBar(Bar bar)
    {
        if (bar.Symbol != _symbol)
            return null;

        UpdateEma(ref _emaFast, EMA_FAST_PERIOD, bar.Close);
        UpdateEma(ref _emaSlow, EMA_SLOW_PERIOD, bar.Close);
        UpdateAtr(bar);
        UpdateRsi(bar.Close);

        if (!IndicatorsReady())
            return null;

        if (_inPosition)
            return ManageOpenTrade(bar);

        if (ShouldEnterTrade(bar))
            return EnterTrade(bar);

        return null;
    }

    #region Signal
    private bool IsUptrend(decimal price) => _emaFast > _emaSlow && price > _emaSlow;
    private bool IsPullback(decimal price) => Math.Abs(price - _emaFast.Value) <= PULLBACK_ATR_BAND * _atr.Value;
    private bool IsMomentumPositive() => _previousRsi != null && _currentRsi >= RSI_MIN_LEVEL && _currentRsi > _previousRsi;

    private bool ShouldEnterTrade(Bar bar) =>
        IsUptrend(bar.Close) && IsPullback(bar.Close) && IsMomentumPositive();
    #endregion

    #region Trade management
    private Order EnterTrade(Bar bar)
    {
        decimal stop = bar.Close - STOP_ATR_MULTIPLIER * _atr.Value;
        decimal risk = bar.Close - stop;
        decimal target = bar.Close + RISK_REWARD_RATIO * risk;

        _stopPrice = stop;
        _targetPrice = target;
        _inPosition = true;

        return new Order
        {
            Id = Guid.NewGuid(),
            Symbol = _symbol,
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 1,
            ReferencePrice = bar.Close
        };
    }

    private Order ManageOpenTrade(Bar bar)
    {
        if (bar.Low <= _stopPrice || bar.High >= _targetPrice)
            return ExitTrade(bar.Close);

        return null;
    }

    private Order ExitTrade(decimal price)
    {
        _inPosition = false;
        _stopPrice = null;
        _targetPrice = null;

        return new Order
        {
            Id = Guid.NewGuid(),
            Symbol = _symbol,
            Side = OrderSide.Sell,
            Type = OrderType.Market,
            Quantity = 1,
            ReferencePrice = price
        };
    }
    #endregion

    #region Indicators
    private bool IndicatorsReady() =>
        _emaFast != null && _emaSlow != null && _atr != null && _currentRsi != null;

    private static void UpdateEma(ref decimal? ema, int period, decimal price)
    {
        if (ema == null) { ema = price; return; }
        decimal k = 2m / (period + 1);
        ema = price * k + ema.Value * (1 - k);
    }

    private void UpdateAtr(Bar bar)
    {
        if (_previousBar == null) { _previousBar = bar; return; }

        decimal tr = Math.Max(bar.High - bar.Low,
                    Math.Max(Math.Abs(bar.High - _previousBar.Close),
                             Math.Abs(bar.Low - _previousBar.Close)));

        _atr = _atr == null ? tr : (_atr.Value * (ATR_PERIOD - 1) + tr) / ATR_PERIOD;
        _previousBar = bar;
    }

    private void UpdateRsi(decimal close)
    {
        if (_previousClose == null) { _previousClose = close; return; }

        decimal change = close - _previousClose.Value;
        decimal gain = Math.Max(change, 0);
        decimal loss = Math.Max(-change, 0);

        if (_averageGain == null)
        {
            _averageGain = gain;
            _averageLoss = loss;
            _previousClose = close;
            return;
        }

        _averageGain = (_averageGain.Value * (RSI_PERIOD - 1) + gain) / RSI_PERIOD;
        _averageLoss = (_averageLoss.Value * (RSI_PERIOD - 1) + loss) / RSI_PERIOD;

        decimal rs = _averageLoss == 0 ? decimal.MaxValue : _averageGain.Value / _averageLoss.Value;
        _previousRsi = _currentRsi;
        _currentRsi = _averageLoss == 0 ? 100 : 100 - (100 / (1 + rs));
        _previousClose = close;
    }
    #endregion
}