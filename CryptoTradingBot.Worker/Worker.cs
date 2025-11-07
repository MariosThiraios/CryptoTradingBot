using CryptoTradingBot.Worker.Services;

namespace CryptoTradingBot.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly BinanceService _binanceService;

    public Worker(ILogger<Worker> logger, BinanceService binanceService)
    {
        _logger = logger;
        _binanceService = binanceService;
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
            var connected = await _binanceService.ConnectMultipleSymbolsAsync("BTCEUR", "ETHEUR", "BNBEUR");

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
            // Disconnect from Binance when stopping
            await _binanceService.DisconnectAsync();
            _logger.LogInformation("Trading Bot Worker stopped at: {time}", DateTimeOffset.Now);
        }
    }
}