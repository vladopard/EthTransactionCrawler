using EthCrawlerApi.Options;
using EthCrawlerApi.Providers.Etherscan.Dto;
using EthCrawlerApi.Providers.Etherscan.Interfaces;
using Microsoft.Extensions.Options;

namespace EthCrawlerApi.Providers.Etherscan
{
    public sealed class EtherscanPaginator : IEtherscanPaginator
    {
        private readonly IEtherscanClient _client;
        private readonly EtherscanOptions _opt;

        public EtherscanPaginator(IEtherscanClient client, IOptions<EtherscanOptions> opt)
        {
            _client = client;
            _opt = opt.Value;
        }

        public Task<IReadOnlyList<TxDto>> GetAllTransactionsAsync(
            string address, long startBlock, int pageSize)
            => FetchAllAsync(
                page => _client.GetTransactionsAsync(address, startBlock, page, pageSize),
                pageSize,
                _opt.DelayMsBetweenPages);

        public Task<IReadOnlyList<InternalDto>> GetAllInternalTransactionsAsync(
            string address, long startBlock, int pageSize)
            => FetchAllAsync(
                page => _client.GetInternalTransactionsAsync(address, startBlock, page, pageSize),
                pageSize,
                _opt.DelayMsBetweenPages);

        public Task<IReadOnlyList<TokenDto>> GetAllTokenTransfersAsync(
            string address, long startBlock, int pageSize)
            => FetchAllAsync(
                page => _client.GetTokenTransfersAsync(address, startBlock, page, pageSize),
                pageSize,
                _opt.DelayMsBetweenPages);

        // ----------------- Generic paginator helper -----------------
        private async Task<IReadOnlyList<T>> FetchAllAsync<T>(
            //prima int i vraca task<nesto> kao param
            Func<int, Task<EtherscanResponse<List<T>>>> fetchPage,
            int pageSize,
            int delayMs,
            int? maxPages = null,
            CancellationToken ct = default)
        {
            var all = new List<T>();
            for (int page = 1; ; page++)
            {
                ct.ThrowIfCancellationRequested();

                var resp = await fetchPage(page);
                var batch = resp.result ?? new List<T>();

                if (batch.Count == 0)
                    break;

                all.AddRange(batch);

                if (batch.Count < pageSize)
                    break;

                if (maxPages.HasValue && page >= maxPages.Value)
                    break;

                if (delayMs > 0)
                    await Task.Delay(delayMs, ct);
            }

            return all;
        }
    }
}
