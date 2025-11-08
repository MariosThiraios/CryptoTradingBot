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

    public void Evaluate24HourPriceChange(string symbol, decimal priceChangePercent)
    {
        if (priceChangePercent > _upThreshold)
        {
           var @event = new SymbolTradeEvent
           {
               Symbol = symbol,
               Direction = TradeDirection.Sell
           };

           PriceThresholdCrossed?.Invoke(this, @event);

           _logger.LogInformation("Price increase detected for {Symbol}: {PriceChangePercent}%. Triggering sell event.",
               symbol, priceChangePercent);

        }
        else if (priceChangePercent < _downThreshold)
        {
            var @event = new SymbolTradeEvent
            {
                Symbol = symbol,
                Direction = TradeDirection.Buy
            };

            PriceThresholdCrossed?.Invoke(this, @event);

            _logger.LogInformation("Price decrease detected for {Symbol}: {PriceChangePercent}%. Triggering buy event.",
                symbol, priceChangePercent);
        }
    }
}