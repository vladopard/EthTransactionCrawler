namespace EthCrawlerApi.Providers.Etherscan.Dto
{
    public class TxDto
    {
        //lowercased zato sto su takva u jsonu
        public required string hash { get; set; }
        public required string blockNumber { get; set; }
        public required string timeStamp { get; set; }//etherscan vraca string
        public required string from { get; set; }
        public required string to { get; set; }
        public required string value { get; set; }
        public required string gasUsed { get; set; }
        public required string gasPrice { get; set; }
        public required string isError { get; set; }
    }

    public class InternalDto
    {
        public required string hash { get; set; }
        public required string blockNumber { get; set; }
        public required string timeStamp { get; set; }
        public required string from { get; set; }
        public required string to { get; set; }
        public required string value { get; set; }
        public required string traceId { get; set; }
    }

    public class TokenDto
    {
        public required string hash { get; set; }
        public required string blockNumber { get; set; }
        public required string timeStamp { get; set; }
        public required string from { get; set; }
        public required string to { get; set; }
        public required string value { get; set; }
        public required string tokenDecimal { get; set; }
        public required string tokenSymbol { get; set; }
        public required string contractAddress { get; set; }
        public required string logIndex { get; set; }
    }

}
