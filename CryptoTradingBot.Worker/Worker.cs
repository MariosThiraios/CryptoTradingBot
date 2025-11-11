using CryptoTradingBot.Worker.Models;
using CryptoTradingBot.Worker.Services;

namespace CryptoTradingBot.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly BinanceWebSocketService _binanceService;
    private readonly BinanceTradingService _tradingService;
    private readonly PriceMonitor _priceMonitor;
    private readonly Dictionary<string, (string QuoteAsset, decimal MinQuoteAmount)> _symbolPairs = new()   
    {
        { "DCR", ("USDT", 5m) },
        { "BTC", ("USDT", 5m) },
        { "FIL", ("USDT", 5m) },
        { "ICP", ("USDT", 5m) }
    };

    public Worker(
        ILogger<Worker> logger, 
        BinanceWebSocketService binanceService, 
        BinanceTradingService tradingService,
        PriceMonitor priceMonitor)
    {
        _logger = logger;
        _binanceService = binanceService;
        _tradingService = tradingService;
        _priceMonitor = priceMonitor;

        // Subscribe to the event
        _priceMonitor.PriceThresholdCrossed += OnPriceThresholdCrossed;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Trading Bot Worker started at: {time}", DateTimeOffset.Now);

        try
        {
            string[] symbols = _symbolPairs
                .Select(kvp => kvp.Key + kvp.Value.QuoteAsset)
                .ToArray();

            // Connect to Binance WebSocket

            // Subscribe to a single symbol
            //var connected = await _binanceService.ConnectAsync("BTCEUR");

            // Subscribe to multiple symbols
            var connected = await _binanceService.ConnectToMultipleSymbolsAsync(symbols);

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

    private async void OnPriceThresholdCrossed(object? sender, SymbolTradeEvent @event)
    {
        _logger.LogInformation("Executing {Direction} trade for {Symbol}", @event.Direction, @event.Symbol);

        try
        {
            // Find the configuration for this symbol
            var symbolConfig = _symbolPairs.FirstOrDefault(kvp => 
                @event.Symbol.Equals(kvp.Key + kvp.Value.QuoteAsset, StringComparison.OrdinalIgnoreCase));

            if (symbolConfig.Key == null)
            {
                _logger.LogWarning("Unknown symbol {Symbol}, not found in configured pairs", @event.Symbol);
                return;
            }

            // Execute the market order based on direction
            bool success = false;

            if (@event.Direction == TradeDirection.Buy)
            {
                success = await _tradingService.ExecuteMarketOrderAsync(@event.Symbol, TradeDirection.Buy, quoteOrderQuantity: symbolConfig.Value.MinQuoteAmount);
            }
            else if (@event.Direction == TradeDirection.Sell)
            {
                success = await _tradingService.ExecuteMarketOrderAsync(@event.Symbol, TradeDirection.Sell, quoteOrderQuantity: symbolConfig.Value.MinQuoteAmount);
            }

            if (success)
            {
                _logger.LogInformation("Successfully executed {Direction} trade for {Symbol}", @event.Direction, @event.Symbol);
            }
            else
            {
                _logger.LogError("Failed to execute {Direction} trade for {Symbol}", @event.Direction, @event.Symbol);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing trade for {Symbol}", @event.Symbol);
        }
    }
}