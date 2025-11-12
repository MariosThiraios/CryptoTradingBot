using Binance.Net.Clients;
using Binance.Net.Enums;
using CryptoExchange.Net.Authentication;
using CryptoTradingBot.Worker.Models;

namespace CryptoTradingBot.Worker.Services;

public class BinanceTradingService
{
    private readonly ILogger<BinanceTradingService> _logger;
    private readonly BinanceRestClient _restClient;

    public BinanceTradingService(
        ILogger<BinanceTradingService> logger,
        IConfiguration configuration)
    {
        _logger = logger;

        // Get API credentials from configuration (appsettings.json or user secrets)
        var apiKey = configuration["Binance:ApiKey"];
        var apiSecret = configuration["Binance:ApiSecret"];

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
        {
            _logger.LogWarning("Binance API credentials not configured. Trading operations will not be available.");
            _restClient = new BinanceRestClient();
        }
        else
        {
            // Create REST client with credentials
            _restClient = new BinanceRestClient(options =>
            {
                options.ApiCredentials = new ApiCredentials(apiKey, apiSecret);
            });
        }
    }

    public async Task<bool> ExecuteMarketOrderAsync(string symbol, TradeDirection direction, decimal? quantity = null, decimal? quoteOrderQuantity = null)
    {
        try
        {
            var orderSide = direction == TradeDirection.Buy ? OrderSide.Buy : OrderSide.Sell;

            // Place the market order
            var orderResult = await _restClient.SpotApi.Trading.PlaceOrderAsync(symbol: symbol, side: orderSide, type: SpotOrderType.Market, quoteQuantity: quoteOrderQuantity!.Value);

            if (!orderResult.Success)
            {
                _logger.LogError("Failed to place market {Direction} order for {Symbol}: {Error}",
                    direction, symbol, orderResult.Error?.Message);
                return false;
            }

            var order = orderResult.Data;
            _logger.LogInformation(
                "Market {Direction} order executed successfully for {Symbol} - OrderId: {OrderId}, Status: {Status}, ExecutedQty: {ExecutedQty}, CummulativeQuoteQty: {CummulativeQuoteQty}",
                direction, symbol, order.Id, order.Status, order.QuantityFilled, order.QuoteQuantityFilled);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing market {Direction} order for {Symbol}", direction, symbol);
            return false;
        }
    }
}