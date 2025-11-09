using CryptoTradingBot.Worker.Models;
using CryptoTradingBot.Worker.Services;

namespace CryptoTradingBot.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly BinanceWebSocketService _binanceService;
    private readonly BinanceAccountService _accountService;
    private readonly PriceMonitor _priceMonitor;

    public Worker(ILogger<Worker> logger, BinanceWebSocketService binanceService, BinanceAccountService accountService, PriceMonitor priceMonitor)
    {
        _logger = logger;
        _binanceService = binanceService;
        _accountService = accountService;
        _priceMonitor = priceMonitor;

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
            var connected = await _binanceService.ConnectToMultipleSymbolsAsync("DCRUSDT", "BTCUSDT", "FILUSDT", "ICPUSDT");

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

    private async void OnPriceThresholdCrossed(object? sender, SymbolTradeEvent e)
    {
        _logger.LogInformation("Executing {Direction} trade for {Symbol}", e.Direction, e.Symbol);

        // Execute the trade
        // e.g., _binanceService.ExecuteTrade(e.Symbol, e.Direction);
    }
}