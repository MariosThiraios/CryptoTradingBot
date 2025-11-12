using Binance.Net.Clients;
using Binance.Net.Interfaces;
using CryptoExchange.Net.Objects.Sockets;
using CryptoTradingBot.Worker.Configuration;
using Microsoft.Extensions.Options;

namespace CryptoTradingBot.Worker.Services;

public class BinanceWebSocketService
{
    private readonly ILogger<BinanceWebSocketService> _logger;
    private readonly PriceMonitor _priceMonitor;
    private BinanceSocketClient? _socketClient;
    private UpdateSubscription? _subscription;
    private Dictionary<string, DateTime> _ignoredSymbols = new();
    private readonly TimeSpan _ignoreExpiration;

    public BinanceWebSocketService(ILogger<BinanceWebSocketService> logger, PriceMonitor priceMonitor, IOptions<TradingConfiguration> tradingConfig)
    {
        _logger = logger;
        _priceMonitor = priceMonitor;
        _ignoreExpiration = TimeSpan.FromHours(tradingConfig.Value.IgnoreExpirationHours);
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
        if (_ignoredSymbols.Count > 0)
        {
            var expiredSymbols = _ignoredSymbols.Where(kvp => DateTime.UtcNow - kvp.Value >= _ignoreExpiration).Select(kvp => kvp.Key).ToList();

            foreach (var symbol in expiredSymbols)
            {
                _ignoredSymbols.Remove(symbol);
            }

            if (_ignoredSymbols.ContainsKey(tickerData.Symbol))
            {
                return;
            }
        }

        _logger.LogInformation(
            "Price Update - Symbol: {Symbol} | Price: {Price} | 24h Change: {Change}% | Volume: {Volume}",
            tickerData.Symbol,
            tickerData.LastPrice,
            tickerData.PriceChangePercent.ToString("F2"),
            tickerData.Volume.ToString("N2")
        );

        var symbolToIgnore = _priceMonitor.Evaluate24HourPriceChange(tickerData.Symbol, tickerData.PriceChangePercent);

        if (symbolToIgnore != null)
        {
            _ignoredSymbols[symbolToIgnore] = DateTime.UtcNow;
        }
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