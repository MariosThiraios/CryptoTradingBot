namespace CryptoTradingBot.Worker.Models
{
    public class SymbolTradeEvent
    {
        public string Symbol { get; set; }
        public TradeDirection Direction { get; set; }
    }

    public enum TradeDirection
    {
        Buy,
        Sell
    }
}