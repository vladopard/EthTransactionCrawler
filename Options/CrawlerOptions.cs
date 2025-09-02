namespace EthCrawlerApi.Options
{
    public class CrawlerOptions
    {
        //Addresses → Листа Ethereum адреса које ће твој crawler да прати
        public required List<string> Addresses { get; set; } = new List<string>();
        //CrawlIntervalMinutes → Подешава колико често (у минутима) crawler
        //треба да покреће нову итерацију. Default је 5 минута.
        public int CrawlIntervalMinutes { get; set; } = 5;
    }
}
