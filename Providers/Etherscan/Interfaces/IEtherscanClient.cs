using EthCrawlerApi.Providers.Etherscan.Dto;

namespace EthCrawlerApi.Providers.Etherscan.Interfaces
{
    public interface IEtherscanClient
    {
        Task<EtherscanResponse<List<TxDto>>> GetTransactionsAsync
            (string address, long startBlock, int page, int size);
        Task<EtherscanResponse<List<InternalDto>>> GetInternalTransactionsAsync
            (string address, long startBlock, int page, int pageSize);
        Task<EtherscanResponse<List<TokenDto>>> GetTokenTransfersAsync
            (string address, long startBlock, int page, int pageSize);
    }
}
