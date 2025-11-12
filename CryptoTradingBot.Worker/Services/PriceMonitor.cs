using CryptoTradingBot.Worker.Models;
using CryptoTradingBot.Worker.Configuration;
using Microsoft.Extensions.Options;

namespace CryptoTradingBot.Worker.Services;

public class PriceMonitor
{
    private readonly ILogger<PriceMonitor> _logger;
    private readonly decimal _upThreshold;
    private readonly decimal _downThreshold;

    public event EventHandler<SymbolTradeEvent>? PriceThresholdCrossed;

    public PriceMonitor(ILogger<PriceMonitor> logger, IOptions<TradingConfiguration> tradingConfig)
    {
        _logger = logger;
        _upThreshold = tradingConfig.Value.UpThreshold;
        _downThreshold = tradingConfig.Value.DownThreshold;
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