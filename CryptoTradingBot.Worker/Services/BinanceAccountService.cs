using Binance.Net.Clients;
using CryptoExchange.Net.Authentication;

namespace CryptoTradingBot.Worker.Services;

public class BinanceAccountService
{
    private readonly ILogger<BinanceAccountService> _logger;
    private readonly BinanceRestClient _restClient;

    public BinanceAccountService(ILogger<BinanceAccountService> logger, IConfiguration configuration)
    {
        _logger = logger;

        // Get API credentials from configuration (appsettings.json or user secrets)
        var apiKey = configuration["Binance:ApiKey"];
        var apiSecret = configuration["Binance:ApiSecret"];

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
        {
            _logger.LogWarning("Binance API credentials not configured. Account operations will not be available.");
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

    // Gets the available balance for a specific asset (e.g., "BTC", "EUR", "USDT")
    public async Task<decimal?> GetAvailableBalanceAsync(string asset)
    {
        try
        {
            var accountInfo = await _restClient.SpotApi.Account.GetAccountInfoAsync();

            if (!accountInfo.Success)
            {
                _logger.LogError("Failed to get account info: {Error}", accountInfo.Error?.Message);
                return null;
            }

            var balance = accountInfo.Data.Balances
                .FirstOrDefault(b => b.Asset.Equals(asset, StringComparison.OrdinalIgnoreCase));

            if (balance == null)
            {
                _logger.LogWarning("Asset {Asset} not found in account", asset);
                return 0;
            }

            _logger.LogDebug("Balance for {Asset}: Available={Available}, Locked={Locked}", 
                asset, balance.Available, balance.Locked);

            return balance.Available;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balance for {Asset}", asset);
            return null;
        }
    }

    // Checks if there's sufficient balance for a trade
    public async Task<bool> HasSufficientBalanceAsync(string symbol, string tradeDirection, decimal minimumBalance)
    {
        try
        {
            // Parse the symbol to determine which asset to check
            // Examples: BTCEUR -> check EUR for buy, BTC for sell
            //           ETHUSDT -> check USDT for buy, ETH for sell
            var (baseAsset, quoteAsset) = ParseSymbol(symbol);

            // For BUY: check if we have enough quote asset (e.g., EUR, USDT)
            // For SELL: check if we have enough base asset (e.g., BTC, ETH)
            var assetToCheck = tradeDirection.Equals("Buy", StringComparison.OrdinalIgnoreCase) 
                ? quoteAsset 
                : baseAsset;

            var balance = await GetAvailableBalanceAsync(assetToCheck);

            if (balance == null)
            {
                _logger.LogError("Could not retrieve balance for {Asset}", assetToCheck);
                return false;
            }

            var hasSufficientBalance = balance.Value >= minimumBalance;

            if (hasSufficientBalance)
            {
                _logger.LogInformation("Sufficient balance for {Direction} {Symbol}: {Asset}={Balance}", 
                    tradeDirection, symbol, assetToCheck, balance.Value);
            }
            else
            {
                _logger.LogWarning("Insufficient balance for {Direction} {Symbol}: {Asset}={Balance} (Required: {Required})", 
                    tradeDirection, symbol, assetToCheck, balance.Value, minimumBalance);
            }

            return hasSufficientBalance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking balance for {Symbol} {Direction}", symbol, tradeDirection);
            return false;
        }
    }

    /// <summary>
    /// Gets all non-zero balances
    /// </summary>
    public async Task<Dictionary<string, decimal>?> GetAllBalancesAsync()
    {
        try
        {
            var accountInfo = await _restClient.SpotApi.Account.GetAccountInfoAsync();

            if (!accountInfo.Success)
            {
                _logger.LogError("Failed to get account info: {Error}", accountInfo.Error?.Message);
                return null;
            }

            var balances = accountInfo.Data.Balances
                .Where(b => b.Available > 0 || b.Locked > 0)
                .ToDictionary(b => b.Asset, b => b.Available + b.Locked);

            return balances;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all balances");
            return null;
        }
    }

    /// <summary>
    /// Parses a trading symbol into base and quote assets
    /// Examples: BTCEUR -> (BTC, EUR), ETHUSDT -> (ETH, USDT)
    /// </summary>
    private (string baseAsset, string quoteAsset) ParseSymbol(string symbol)
    {
        // Common quote assets
        var quoteAssets = new[] { "USDT", "BUSD", "EUR", "USD", "BTC", "ETH", "BNB" };

        foreach (var quote in quoteAssets)
        {
            if (symbol.EndsWith(quote, StringComparison.OrdinalIgnoreCase))
            {
                var baseAsset = symbol.Substring(0, symbol.Length - quote.Length);
                return (baseAsset, quote);
            }
        }

        // Fallback: assume last 3-4 characters are quote asset
        if (symbol.Length > 6)
        {
            var baseAsset = symbol.Substring(0, symbol.Length - 4);
            var quoteAsset = symbol.Substring(symbol.Length - 4);
            return (baseAsset, quoteAsset);
        }

        var fallbackBase = symbol.Substring(0, 3);
        var fallbackQuote = symbol.Substring(3);
        return (fallbackBase, fallbackQuote);
    }
}
