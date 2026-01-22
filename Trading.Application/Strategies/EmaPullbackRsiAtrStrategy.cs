using Trading.Application.Services;
using Trading.Domain.Entities;
using Trading.Domain.Enums;
using Trading.Domain.Interfaces;

namespace Trading.Application.Strategies;

public class EmaPullbackRsiAtrStrategy : IStrategy
{
    #region Properties
    private const int EMA_FAST_PERIOD = 20;
    private const int EMA_SLOW_PERIOD = 50;

    private const int RSI_PERIOD = 14;
    private const decimal RSI_MIN_LEVEL = 52m;

    private const int ATR_PERIOD = 14;
    private const decimal PULLBACK_ATR_BAND = 0.35m;

    private const decimal STOP_ATR_MULTIPLIER = 2.2m;
    private const decimal RISK_REWARD_RATIO = 2.5m;

    private const int MAX_HOLD_DAYS = 15;
    private const int COOLDOWN_BARS = 1;

    private readonly string _symbol;
    private readonly IRiskManager _riskManager;
    private readonly Account _account;
    private int _quantity;

    private decimal? _emaFast;
    private decimal? _emaSlow;
    private decimal? _atr;
    private Bar _previousBar;

    private decimal? _averageGain;
    private decimal? _averageLoss;
    private decimal? _previousClose;
    private decimal? _currentRsi;
    private decimal? _previousRsi;

    private int _barIndex;

    private decimal? _entryPrice;
    private decimal? _stopPrice;
    private decimal? _targetPrice;
    private DateTime _entryTime;

    private int _cooldownUntil;

    const int BAR_DURATION_MIN = 15;
    private readonly BarBuilder _barBuilder = new(TimeSpan.FromMinutes(BAR_DURATION_MIN));

    const decimal QTE_RISK_PERCENT = 100;
    private readonly FixedRiskPositionSizer _positionSizer = new(QTE_RISK_PERCENT);
    #endregion

    public EmaPullbackRsiAtrStrategy(
        string symbol,
        IRiskManager riskManager,
        Account account)
    {
        _symbol = symbol;
        _riskManager = riskManager;
        _account = account;
    }

    public Order DecideActionFromQuote(Quote quote)
    {
        Bar closedBar = _barBuilder.ProcessQuote(quote);

        if (closedBar == null)
            return null; // bar not finished yet

        return DecideActionFromBar(closedBar);
    }

    public Order DecideActionFromBar(Bar bar)
    {
        if (bar.Symbol != _symbol)
            return null;

        _barIndex++;

        UpdateEma(ref _emaFast, EMA_FAST_PERIOD, bar.Close);
        UpdateEma(ref _emaSlow, EMA_SLOW_PERIOD, bar.Close);
        UpdateAtr(bar);
        UpdateRsi(bar.Close);

        if (!IndicatorsReady())
        {
            _previousBar = bar;
            return null;
        }

        if (_barIndex <= _cooldownUntil)
        {
            _previousBar = bar;
            return null;
        }

        if (_entryPrice != null)
            return ManageOpenTrade(bar);

        if (ShouldEnterTrade(bar))
            return EnterTrade(bar);

        _previousBar = bar;
        return null;
    }

    private bool IndicatorsReady()
    {
        return _emaFast != null &&
               _emaSlow != null &&
               _atr != null &&
               _currentRsi != null;
    }

    private bool IsUptrend(decimal price)
    {
        return _emaFast > _emaSlow && price > _emaSlow;
    }

    private bool IsPullback(decimal price)
    {
        decimal distance = Math.Abs(price - _emaFast.Value);
        decimal allowed = PULLBACK_ATR_BAND * _atr.Value;
        return distance <= allowed && price >= _emaFast;
    }

    private bool IsMomentumPositive()
    {
        if (_previousRsi == null)
            return false;

        return _currentRsi >= RSI_MIN_LEVEL && _currentRsi > _previousRsi;
    }

    private bool ShouldEnterTrade(Bar bar)
    {
        return IsUptrend(bar.Close) &&
               IsPullback(bar.Close) &&
               IsMomentumPositive();
    }

    private Order EnterTrade(Bar bar)
    {
        decimal entry = bar.Close;
        decimal stop = entry - STOP_ATR_MULTIPLIER * _atr.Value;
        decimal risk = entry - stop;
        decimal target = entry + RISK_REWARD_RATIO * risk;

        _quantity = _positionSizer.CalculateQuantity(_account, entry, stop);

        var buy = CreateOrder(OrderSide.Buy);

        if (!_riskManager.IsOrderAllowed(buy))
            return null;

        _entryPrice = entry;
        _stopPrice = stop;
        _targetPrice = target;
        _entryTime = bar.Timestamp;
        _cooldownUntil = _barIndex + COOLDOWN_BARS;

        return buy;
    }

    private Order ManageOpenTrade(Bar bar)
    {
        if (bar.Low <= _stopPrice)
            return ExitTrade();

        if (bar.High >= _targetPrice)
            return ExitTrade();

        if ((bar.Timestamp - _entryTime).TotalDays >= MAX_HOLD_DAYS)
            return ExitTrade();

        _previousBar = bar;
        return null;
    }

    private Order ExitTrade()
    {
        Order sell = CreateOrder(OrderSide.Sell);

        if (!_riskManager.IsOrderAllowed(sell))
            return null;

        ResetPosition();
        _cooldownUntil = _barIndex + COOLDOWN_BARS;

        return sell;
    }

    private void ResetPosition()
    {
        _entryPrice = null;
        _stopPrice = null;
        _targetPrice = null;
    }

    private static void UpdateEma(ref decimal? ema, int period, decimal price)
    {
        if (ema == null)
        {
            ema = price;
            return;
        }

        decimal k = 2m / (period + 1);
        ema = price * k + ema.Value * (1 - k);
    }

    private void UpdateAtr(Bar bar)
    {
        if (_previousBar == null)
            return;

        decimal prevClose = _previousBar.Close;

        decimal range1 = bar.High - bar.Low;
        decimal range2 = Math.Abs(bar.High - prevClose);
        decimal range3 = Math.Abs(bar.Low - prevClose);

        decimal trueRange = Math.Max(range1, Math.Max(range2, range3));

        if (_atr == null)
            _atr = trueRange;
        else
            _atr = (_atr.Value * (ATR_PERIOD - 1) + trueRange) / ATR_PERIOD;
    }

    private void UpdateRsi(decimal close)
    {
        if (_previousClose == null)
        {
            _previousClose = close;
            return;
        }

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

        _averageGain =
            (_averageGain.Value * (RSI_PERIOD - 1) + gain) / RSI_PERIOD;

        _averageLoss =
            (_averageLoss.Value * (RSI_PERIOD - 1) + loss) / RSI_PERIOD;

        decimal rs = _averageLoss == 0
            ? decimal.MaxValue
            : _averageGain.Value / _averageLoss.Value;

        _previousRsi = _currentRsi;
        _currentRsi = 100 - (100 / (1 + rs));

        _previousClose = close;
    }

    private Order CreateOrder(OrderSide side)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            Symbol = _symbol,
            Side = side,
            Type = OrderType.Market,
            Quantity = _quantity
        };
    }
}