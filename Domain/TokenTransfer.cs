namespace EthCrawlerApi.Domain
{
    public class TokenTransfer
    {
        public required string UniqueId { get; set; }
        // Јединствени кључ за token transfer:
        // комбинација TxHash-а и LogIndex-а (јер у једном TX може бити више трансфера)

        public required string TxHash { get; set; }
        // Hash трансакције у којој се одиграо transfer

        public required long BlockNumber { get; set; }
        // Број блока у коме је transfer забележен

        public required DateTime TimeStampUtc { get; set; }
        // Време у UTC формату када је блок верификован

        public required string ContractAddress { get; set; }
        // Адреса смарт уговора токена (нпр. USDT, DAI…)

        public required string TokenSymbol { get; set; }
        // Ознака токена (нпр. USDT, DAI, LINK)

        public required int TokenDecimals { get; set; }
        // Број децимала за тај токен (битно за нормализацију Amount-а)

        public required string From { get; set; }
        // Адреса пошиљаоца (од кога токени одлазе)

        public required string To { get; set; }
        // Адреса примаоца (коме токени стижу)

        public required decimal Amount { get; set; }
        // Нормализован износ (примењен TokenDecimals, тако да је у „људском“ формату)
    }
}
