using Binance.Net.Clients;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Models.Spot;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.Sockets;

namespace CryptoTradingBot.Worker.Services;

public class BinanceService
{
    private readonly ILogger<BinanceService> _logger;
    private BinanceSocketClient? _socketClient;
    private UpdateSubscription? _subscription;

    public BinanceService(ILogger<BinanceService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ConnectAsync(string symbol = "BTCUSDT")
    {
        try
        {
            _logger.LogInformation("Connecting to Binance WebSocket for {Symbol}...", symbol);

            // Create socket client
            _socketClient = new BinanceSocketClient();

            // Subscribe to ticker updates (price changes)
            var subscribeResult = await _socketClient.SpotApi.ExchangeData
                .SubscribeToTickerUpdatesAsync(symbol, data =>
                {
                    OnPriceUpdate(data.Data);
                });

            if (subscribeResult.Success)
            {
                _subscription = subscribeResult.Data;
                _logger.LogInformation("Successfully connected to Binance WebSocket for {Symbol}", symbol);
                return true;
            }
            else
            {
                _logger.LogError("Failed to connect to Binance: {Error}", subscribeResult.Error?.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to Binance WebSocket");
            return false;
        }
    }

    private void OnPriceUpdate(IBinanceTick tickerData)
    {
        // This method is called every time there's a price update
        _logger.LogInformation(
            "Price Update - Symbol: {Symbol} | Price: {Price} | 24h Change: {Change}% | Volume: {Volume}",
            tickerData.Symbol,
            tickerData.LastPrice,
            tickerData.PriceChangePercent.ToString("F2"),
            tickerData.Volume.ToString("N2")
        );

        // Here you can add your trading strategy logic
        // For example:
        // - Check if price crosses certain thresholds
        // - Calculate indicators (RSI, MACD, etc.)
        // - Trigger buy/sell signals
    }

    public async Task DisconnectAsync()
    {
        try
        {
            if (_subscription != null)
            {
                await _subscription.CloseAsync();
                _logger.LogInformation("Disconnected from Binance WebSocket");
            }

            _socketClient?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from Binance");
        }
    }

    // Additional method to subscribe to multiple symbols
    public async Task<bool> ConnectMultipleSymbolsAsync(params string[] symbols)
    {
        try
        {
            _logger.LogInformation("Connecting to Binance WebSocket for multiple symbols: {Symbols}",
                string.Join(", ", symbols));

            _socketClient = new BinanceSocketClient();

            var subscribeResult = await _socketClient.SpotApi.ExchangeData
                .SubscribeToTickerUpdatesAsync(symbols, data =>
                {
                    OnPriceUpdate(data.Data);
                });

            if (subscribeResult.Success)
            {
                _subscription = subscribeResult.Data;
                _logger.LogInformation("Successfully connected to Binance for multiple symbols");
                return true;
            }
            else
            {
                _logger.LogError("Failed to connect: {Error}", subscribeResult.Error?.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to Binance WebSocket");
            return false;
        }
    }
}