using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        // Za TEST: ograniči na prvu stranicu (maxPages: 1) i dodaj throttle (delayMs).
        public Task<IReadOnlyList<TxDto>> GetAllTransactionsAsync(
            string address, long startBlock, int pageSize)
            => FetchAllAsync(
                page => _client.GetTransactionsAsync(address, startBlock, page, pageSize),
                pageSize,
                delayMs: _opt.DelayMsBetweenPages > 0 ? _opt.DelayMsBetweenPages : 1200,
                maxPages: 1);

        public Task<IReadOnlyList<InternalDto>> GetAllInternalTransactionsAsync(
            string address, long startBlock, int pageSize)
            => FetchAllAsync(
                page => _client.GetInternalTransactionsAsync(address, startBlock, page, pageSize),
                pageSize,
                delayMs: _opt.DelayMsBetweenPages > 0 ? _opt.DelayMsBetweenPages : 1200,
                maxPages: 1);

        public Task<IReadOnlyList<TokenDto>> GetAllTokenTransfersAsync(
            string address, long startBlock, int pageSize)
            => FetchAllAsync(
                page => _client.GetTokenTransfersAsync(address, startBlock, page, pageSize),
                pageSize,
                delayMs: _opt.DelayMsBetweenPages > 0 ? _opt.DelayMsBetweenPages : 1200,
                maxPages: 1);

        // ----------------- Generic paginator helper -----------------
        public async Task<IReadOnlyList<T>> FetchAllAsync<T>(
            Func<int, Task<EtherscanResponse<List<T>>>> fetchPage,
            int pageSize,
            int delayMs = 1200,
            int? maxPages = null,
            CancellationToken ct = default)
        {
            var all = new List<T>();
            var page = 1;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                var resp = await fetchPage(page);

                // "No transactions found" -> nema više podataka
                if (resp.status == "0" &&
                    resp.message?.IndexOf("No transactions", StringComparison.OrdinalIgnoreCase) >= 0)
                    break;

                if (resp.status != "1")
                    throw new InvalidOperationException($"Etherscan error: {resp.message}");

                var chunk = resp.result ?? new List<T>();
                all.AddRange(chunk);

                // ako je došlo manje od pageSize -> nema sledeće stranice
                if (chunk.Count < pageSize)
                    break;

                // hard limit na broj stranica (TEST)
                if (maxPages.HasValue && page >= maxPages.Value)
                    break;

                page++;

                // throttle (<= ~1 req/s za free plan)
                if (delayMs > 0)
                    await Task.Delay(delayMs, ct);
            }

            return all;
        }
    }
}
