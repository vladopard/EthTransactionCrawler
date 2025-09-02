using EthCrawlerApi.Domain;

namespace EthCrawlerApi.Repositories
{
    public interface IEthRepository
    {
        Task<int> CountTransactionsAsync(string address, long? fromBlock, long? toBlock);
        Task<List<EthTransaction>> GetTransactionsPageAsync(string address, long? fromBlock, long? toBlock, int page, int pageSize);
        Task<long> GetMaxTxBlockAsync(string address);
        Task BulkUpsertTransactionsAsync(List<EthTransaction> items);

        // ---- Internal ----
        Task<int> CountInternalAsync(string address, long? fromBlock, long? toBlock);
        Task<List<InternalTransaction>> GetInternalPageAsync(string address, long? fromBlock, long? toBlock, int page, int pageSize);
        Task<long> GetMaxInternalBlockAsync(string address);
        Task BulkUpsertInternalAsync(List<InternalTransaction> items);

        // ---- Token Transfers ----
        Task<int> CountTokensAsync(string address, long? fromBlock, long? toBlock);
        Task<List<TokenTransfer>> GetTokensPageAsync(string address, long? fromBlock, long? toBlock, int page, int pageSize);
        Task<long> GetMaxTokenBlockAsync(string address);
        Task BulkUpsertTokensAsync(List<TokenTransfer> items);
    }
}
