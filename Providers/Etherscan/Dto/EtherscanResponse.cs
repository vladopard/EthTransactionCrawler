namespace EthCrawlerApi.Providers.Etherscan.Dto
{
    public class EtherscanResponse<T>
    {
        public required string status { get; set; }
        public required string message { get; set; }
        public T? result { get; set; }

        /* ETHERSCAN ODGOVOR, result moze biti prazan
        {
          "status": "1",
          "message": "OK",
          "result": [
            {
              "blockNumber": "19245000",
              "timeStamp": "1735589150",
              "hash": "0x123...",
              "from": "0xabc...",
              "to": "0xdef...",
              "value": "1000000000000000000"
            }
          ]
        }
        T је generic параметар → може бити било који DTO.

        EtherscanResponse<List<TxDto>>

        EtherscanResponse<List<InternalDto>>

        EtherscanResponse<List<TokenDto>>
        */

    }
}
