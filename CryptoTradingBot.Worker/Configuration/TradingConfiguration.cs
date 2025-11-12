namespace CryptoTradingBot.Worker.Configuration;

public class TradingConfiguration
{
    public const string SectionName = "Trading";
    
    public decimal UpThreshold { get; set; } = 5.0m;
    public decimal DownThreshold { get; set; } = -5.0m;
    public int IgnoreExpirationHours { get; set; } = 6;
    public List<SymbolPairConfig> SymbolPairs { get; set; } = new();
}

public class SymbolPairConfig
{
    public string BaseAsset { get; set; } = string.Empty;
    public string QuoteAsset { get; set; } = string.Empty;
    public decimal MinQuoteAmount { get; set; }
}
