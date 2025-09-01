namespace EthCrawlerApi.Domain
{
    public class InternalTransaction
    {
        public required string UniqueId { get; set; }
        // Јединствени кључ за интерну трансакцију: комбинација TX Hash-а и TraceId-а

        public required string Hash { get; set; }
        // Hash родитељске (external) трансакције у којој се ова интерна јавља

        public required long BlockNumber { get; set; }
        // Број блока у коме је интерна трансакција извршена

        public required DateTime TimeStampUtc { get; set; }
        // Време у UTC формату када је блок верификован

        public required string From { get; set; }
        // Адреса која је иницирала ову интерну трансакцију (нпр. уговор који позива други уговор)

        public required string To { get; set; }
        // Одредишна адреса интерне трансакције (нпр. уговор или корисничка адреса)

        public required decimal ValueEth { get; set; }
        // Количина ETH која је пренета у оквиру ове интерне трансакције
    }
}
