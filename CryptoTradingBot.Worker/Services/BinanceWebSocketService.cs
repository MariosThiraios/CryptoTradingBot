using Binance.Net.Clients;
using Binance.Net.Interfaces;
using CryptoExchange.Net.Objects.Sockets;

namespace CryptoTradingBot.Worker.Services;

public class BinanceWebSocketService
{
    private readonly ILogger<BinanceWebSocketService> _logger;
    private readonly PriceMonitor _priceMonitor;
    private BinanceSocketClient? _socketClient;
    private UpdateSubscription? _subscription;

    public BinanceWebSocketService(ILogger<BinanceWebSocketService> logger, PriceMonitor priceMonitor)
    {
        _logger = logger;
        _priceMonitor = priceMonitor;
    }

    public async Task<bool> ConnectToSingleSymbolAsync(string symbol)
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
                    OnTickerUpdate(data.Data);
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

    // Additional method to subscribe to multiple symbols
    public async Task<bool> ConnectToMultipleSymbolsAsync(params string[] symbols)
    {
        try
        {
            _logger.LogInformation("Connecting to Binance WebSocket for multiple symbols: {Symbols}",
                string.Join(", ", symbols));

            _socketClient = new BinanceSocketClient();

            var subscribeResult = await _socketClient.SpotApi.ExchangeData
                .SubscribeToTickerUpdatesAsync(symbols, data =>
                {
                    OnTickerUpdate(data.Data);
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

    private void OnTickerUpdate(IBinanceTick tickerData)
    {
        // This method is called every time there's a price update
        _logger.LogInformation(
            "Price Update - Symbol: {Symbol} | Price: {Price} | 24h Change: {Change}% | Volume: {Volume}",
            tickerData.Symbol,
            tickerData.LastPrice,
            tickerData.PriceChangePercent.ToString("F2"),
            tickerData.Volume.ToString("N2")
        );

        _priceMonitor.Evaluate24HourPriceChange(tickerData.Symbol, tickerData.PriceChangePercent);
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
}