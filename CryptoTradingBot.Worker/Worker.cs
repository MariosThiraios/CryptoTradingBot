using CryptoTradingBot.Worker.Models;
using CryptoTradingBot.Worker.Services;

namespace CryptoTradingBot.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly BinanceWebSocketService _binanceService;
    private readonly PriceMonitor _priceMonitor;
    private readonly TradeThrottler _tradeThrottler;

    public Worker(ILogger<Worker> logger, BinanceWebSocketService binanceService, PriceMonitor priceMonitor, TradeThrottler tradeThrottler)
    {
        _logger = logger;
        _binanceService = binanceService;
        _priceMonitor = priceMonitor;
        _tradeThrottler = tradeThrottler;

        // Subscribe to the event
        _priceMonitor.PriceThresholdCrossed += OnPriceThresholdCrossed;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Trading Bot Worker started at: {time}", DateTimeOffset.Now);

        try
        {
            // Connect to Binance WebSocket

            // Subscribe to a single symbol
            //var connected = await _binanceService.ConnectAsync("BTCEUR");

            // Subscribe to multiple symbols
            var connected = await _binanceService.ConnectToMultipleSymbolsAsync("BTCEUR", "ETHEUR", "BNBEUR", "ICPUSDT");

            if (!connected)
            {
                _logger.LogError("Failed to connect to Binance. Stopping worker.");
                return;
            }

            // Keep the service running and listening to WebSocket updates
            while (!stoppingToken.IsCancellationRequested)
            {
                // The price updates are received automatically via WebSocket
                // This loop just keeps the service alive
                await Task.Delay(60000, stoppingToken); // Check every minute if we should continue
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Trading Bot Worker is stopping gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred in the Trading Bot");
            throw;
        }
        finally
        {
            // Unsubscribe from event to prevent memory leaks
            _priceMonitor.PriceThresholdCrossed -= OnPriceThresholdCrossed;

            // Disconnect from Binance when stopping
            await _binanceService.DisconnectAsync();
            _logger.LogInformation("Trading Bot Worker stopped at: {time}", DateTimeOffset.Now);
        }
    }

    private void OnPriceThresholdCrossed(object? sender, SymbolTradeEvent e)
    {
        _logger.LogWarning("TRADE SIGNAL: {Direction} {Symbol}", e.Direction, e.Symbol);

        // 1. Check if we can trade (cooldown period has passed)
        if (!_tradeThrottler.CanTrade(e.Symbol))
        {
            var remaining = _tradeThrottler.GetRemainingCooldown(e.Symbol);
            _logger.LogWarning("Trade blocked for {Symbol}. Cooldown remaining: {Remaining}",
                e.Symbol,
                remaining);
            return;
        }

        // 2. Check if we have sufficient balance


        // 3. Execute the trade
        // e.g., _binanceService.ExecuteTrade(e.Symbol, e.Direction);

        _logger.LogInformation("Executing {Direction} trade for {Symbol}", e.Direction, e.Symbol);

        // Record the trade to start the cooldown
        _tradeThrottler.RecordTrade(e.Symbol);
    }
}