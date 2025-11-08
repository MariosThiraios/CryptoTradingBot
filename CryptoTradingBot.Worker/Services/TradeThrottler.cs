namespace CryptoTradingBot.Worker.Services;

public class TradeThrottler
{
    private readonly ILogger<TradeThrottler> _logger;
    private readonly Dictionary<string, DateTime> _lastTradeTime = new();
    private readonly TimeSpan _cooldownPeriod;
    private readonly object _lock = new();

    public TradeThrottler(ILogger<TradeThrottler> logger, TimeSpan? cooldownPeriod = null)
    {
        _logger = logger;
        _cooldownPeriod = cooldownPeriod ?? TimeSpan.FromHours(6); // Default 6 hours
    }

    // Checks if enough time has passed since the last trade for this symbol
    public bool CanTrade(string symbol)
    {
        lock (_lock)
        {
            if (!_lastTradeTime.TryGetValue(symbol, out var lastTradeTime))
            {
                // No previous trade recorded
                return true;
            }

            var timeSinceLastTrade = DateTime.UtcNow - lastTradeTime;
            var canTrade = timeSinceLastTrade >= _cooldownPeriod;

            if (!canTrade)
            {
                var remainingTime = _cooldownPeriod - timeSinceLastTrade;
                _logger.LogInformation(
                    "Trade cooldown active for {Symbol}. Last trade: {LastTrade}. Time remaining: {Remaining}",
                    symbol,
                    lastTradeTime.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    FormatTimeSpan(remainingTime));
            }

            return canTrade;
        }
    }

    // Records a trade timestamp for a symbol
    public void RecordTrade(string symbol)
    {
        lock (_lock)
        {
            var tradeTime = DateTime.UtcNow;
            _lastTradeTime[symbol] = tradeTime;
            _logger.LogInformation("Trade recorded for {Symbol} at {TradeTime}", symbol, tradeTime.ToString("yyyy-MM-dd HH:mm:ss UTC"));
        }
    }

    // Gets the time of the last trade for a symbol, if any
    public DateTime? GetLastTradeTime(string symbol)
    {
        lock (_lock)
        {
            return _lastTradeTime.TryGetValue(symbol, out var lastTradeTime) ? lastTradeTime : null;
        }
    }

    // Gets the remaining cooldown time for a symbol
    public TimeSpan? GetRemainingCooldown(string symbol)
    {
        lock (_lock)
        {
            if (!_lastTradeTime.TryGetValue(symbol, out var lastTradeTime))
            {
                return null;
            }

            var timeSinceLastTrade = DateTime.UtcNow - lastTradeTime;
            var remaining = _cooldownPeriod - timeSinceLastTrade;

            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalHours >= 1)
            return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
        if (timeSpan.TotalMinutes >= 1)
            return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
        return $"{timeSpan.Seconds}s";
    }
}