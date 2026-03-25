using Trading.Domain.Entities;
using Trading.Domain.Enums;
using Trading.Domain.Interfaces;
using Trading.Domain.Services;

namespace Trading.Domain.Strategies;

public class SimpleEmaPullbackAtrStrategy : IStrategy
{
    #region Properties
    private const int EMA_FAST = 20;
    private const int EMA_SLOW = 50;
    private const int ATR_PERIOD = 14;
    private const decimal STOP_ATR_MULTIPLIER = 2.2m;
    private const decimal PULLBACK_ATR_BAND = 0.35m;
    private const int MAX_HOLD_BARS = 20;

    private readonly string _symbol;
    private readonly IRiskManager _riskManager;
    private readonly Account _account;
    private readonly QuantityCalculator _qtyCalculator = new(0.01m);

    private decimal? _emaFast;
    private decimal? _emaSlow;
    private decimal? _atr;
    private Bar _prevBar;

    private const int RSI_PERIOD = 14;
    private const decimal RSI_MIN_LEVEL = 52m;

    private decimal? _avgGain;
    private decimal? _avgLoss;
    private decimal? _prevClose;
    private decimal? _rsi;

    private decimal? _entryPrice;
    private decimal? _stopPrice;
    private decimal _highestPrice;
    private int _entryBarIndex;
    private int _barIndex;

    private readonly BarBuilder _barBuilder = new(TimeSpan.FromMinutes(15));
    #endregion

    public SimpleEmaPullbackAtrStrategy(
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
        Bar bar = _barBuilder.ProcessQuote(quote);
        if (bar == null) return null;

        _barIndex++;

        UpdateEma(ref _emaFast, EMA_FAST, bar.Close);
        UpdateEma(ref _emaSlow, EMA_SLOW, bar.Close);
        UpdateAtr(bar);
        UpdateRsi(bar.Close);

        if (_emaFast == null || _emaSlow == null || _atr == null || _rsi == null)
        {
            _prevBar = bar;
            return null;
        }

        if (_entryPrice != null)
            return ManageOpenTrade(bar);

        if (ShouldEnter(bar))
            return Enter(bar);

        _prevBar = bar;
        return null;
    }

    private bool ShouldEnter(Bar bar)
    {
        bool uptrend = _emaFast > _emaSlow;
        bool momentumOk = _rsi >= RSI_MIN_LEVEL;

        decimal distance = Math.Abs(bar.Close - _emaFast.Value);
        bool pullback = distance <= PULLBACK_ATR_BAND * _atr.Value;

        return uptrend && momentumOk && pullback;
    }

    private Order Enter(Bar bar)
    {
        decimal entry = bar.Close;
        decimal stop = entry - STOP_ATR_MULTIPLIER * _atr.Value;

        int qty = _qtyCalculator.Calculate(_account, entry, stop);
        if (qty <= 0) return null;

        Order buy = new()
        {
            Symbol = _symbol,
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = qty,
            ReferencePrice = entry
        };

        buy = _riskManager.AdjustOrder(buy);
        if (buy == null) return null;

        _entryPrice = entry;
        _stopPrice = stop;
        _highestPrice = entry;
        _entryBarIndex = _barIndex;

        return buy;
    }

    private Order ManageOpenTrade(Bar bar)
    {
        _highestPrice = Math.Max(_highestPrice, bar.High);

        decimal newStop =
            _highestPrice - STOP_ATR_MULTIPLIER * _atr.Value;

        if (newStop > _stopPrice)
            _stopPrice = newStop;

        if (bar.Low <= _stopPrice)
            return Exit(_stopPrice.Value);

        if (_barIndex - _entryBarIndex >= MAX_HOLD_BARS)
            return Exit(bar.Close);

        _prevBar = bar;
        return null;
    }

    private Order Exit(decimal price)
    {
        if (!_account.Positions.TryGetValue(_symbol, out var pos))
            return null;

        int qty = Math.Abs(pos.NetQuantity);
        if (qty <= 0) return null;

        Order sell = new()
        {
            Symbol = _symbol,
            Side = OrderSide.Sell,
            Type = OrderType.Market,
            Quantity = qty,
            ReferencePrice = price
        };

        sell = _riskManager.AdjustOrder(sell);
        if (sell == null) return null;

        _entryPrice = null;
        _stopPrice = null;

        return sell;
    }

    private void UpdateEma(ref decimal? ema, int period, decimal price)
    {
        if (ema == null) { ema = price; return; }
        decimal k = 2m / (period + 1);
        ema = price * k + ema.Value * (1 - k);
    }

    private void UpdateAtr(Bar bar)
    {
        if (_prevBar == null) return;

        decimal tr = Math.Max(
            bar.High - bar.Low,
            Math.Max(
                Math.Abs(bar.High - _prevBar.Close),
                Math.Abs(bar.Low - _prevBar.Close)));

        _atr ??= tr;
        _atr = (_atr.Value * (ATR_PERIOD - 1) + tr) / ATR_PERIOD;
    }

    private void UpdateRsi(decimal close)
    {
        if (_prevClose == null)
        {
            _prevClose = close;
            return;
        }

        decimal change = close - _prevClose.Value;
        decimal gain = Math.Max(change, 0);
        decimal loss = Math.Max(-change, 0);

        if (_avgGain == null)
        {
            _avgGain = gain;
            _avgLoss = loss;
            _prevClose = close;
            return;
        }

        _avgGain = (_avgGain.Value * (RSI_PERIOD - 1) + gain) / RSI_PERIOD;
        _avgLoss = (_avgLoss.Value * (RSI_PERIOD - 1) + loss) / RSI_PERIOD;

        if (_avgLoss.Value == 0)
            _rsi = 100m;
        else
        {
            decimal rs = _avgGain.Value / _avgLoss.Value;
            _rsi = 100m - (100m / (1m + rs));
        }

        _prevClose = close;
    }
}