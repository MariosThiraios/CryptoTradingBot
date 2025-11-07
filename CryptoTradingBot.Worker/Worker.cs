namespace CryptoTradingBot.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Trading Bot Worker started at: {time}", DateTimeOffset.Now);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // This is where your trading bot logic will go
                _logger.LogInformation("Bot is running at: {time}", DateTimeOffset.UtcNow);

                // Example of different log levels:
                _logger.LogDebug("Debug: Checking market conditions...");
                _logger.LogInformation("Info: Price check completed");
                // _logger.LogWarning("Warning: High volatility detected");
                // _logger.LogError("Error: Failed to connect to exchange");

                await Task.Delay(5000, stoppingToken); // Run every 5 seconds
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Trading Bot Worker is stopping gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred in the Trading Bot");
            throw; // Re-throw to trigger service restart
        }
        finally
        {
            _logger.LogInformation("Trading Bot Worker stopped at: {time}", DateTimeOffset.Now);
        }
    }
}