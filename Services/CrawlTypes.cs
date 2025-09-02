namespace EthCrawlerApi.Services
{
    [Flags]
    public enum CrawlTypes
    {
        None = 0,
        Transactions = 1,
        Internal = 2,
        TokenTransfers = 4,
        All = Transactions | Internal | TokenTransfers
    }
}
