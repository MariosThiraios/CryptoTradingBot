using CryptoTradingBot.Worker.Models;

namespace CryptoTradingBot.Worker.Services;

public class PriceMonitor
{
    private readonly ILogger<PriceMonitor> _logger;
    private readonly decimal _upThreshold = 10.0m;
    private readonly decimal _downThreshold = -10.0m;

    public event EventHandler<SymbolTradeEvent>? PriceThresholdCrossed;

    public PriceMonitor(ILogger<PriceMonitor> logger)
    {
        _logger = logger;
    }

    public string? Evaluate24HourPriceChange(string symbol, decimal priceChangePercent)
    {
        if (priceChangePercent > _upThreshold)
        {
           var @event = new SymbolTradeEvent
           {
               Symbol = symbol,
               Direction = TradeDirection.Sell
           };

           _logger.LogInformation("Price increase detected for {Symbol}: {PriceChangePercent}%. Triggering sell event.",
               symbol, priceChangePercent);

           PriceThresholdCrossed?.Invoke(this, @event);

            return symbol;
        }
        else if (priceChangePercent < _downThreshold)
        {
            var @event = new SymbolTradeEvent
            {
                Symbol = symbol,
                Direction = TradeDirection.Buy
            };

            _logger.LogInformation("Price decrease detected for {Symbol}: {PriceChangePercent}%. Triggering buy event.",
                symbol, priceChangePercent);

            PriceThresholdCrossed?.Invoke(this, @event);

            return symbol;
        }
        else
        {
            return null;
        }
    }
}