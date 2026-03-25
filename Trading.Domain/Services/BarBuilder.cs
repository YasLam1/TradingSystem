using Trading.Domain.Entities;

namespace Trading.Domain.Services;

public class BarBuilder
{
    private readonly TimeSpan _barDuration;

    private Bar _currentBar;

    public BarBuilder(TimeSpan barDuration)
        => _barDuration = barDuration;

    public Bar ProcessQuote(Quote q)
    {
        DateTime barTime = new(q.Timestamp.Ticks - (q.Timestamp.Ticks % _barDuration.Ticks));

        if (_currentBar == null || _currentBar.Timestamp != barTime)
        {
            Bar finishedBar = _currentBar;

            _currentBar = new Bar
            {
                Symbol = q.Symbol,
                Timestamp = barTime,
                Open = q.LastTradedPrice,
                High = q.LastTradedPrice,
                Low = q.LastTradedPrice,
                Close = q.LastTradedPrice
            };

            return finishedBar;
        }

        UpdateCurrentBar(q);
        return null;
    }

    private void UpdateCurrentBar(Quote q)
    {
        _currentBar.High = Math.Max(_currentBar.High, q.LastTradedPrice);

        _currentBar.Low = Math.Min(_currentBar.Low, q.LastTradedPrice);

        _currentBar.Close = q.LastTradedPrice;
    }
}