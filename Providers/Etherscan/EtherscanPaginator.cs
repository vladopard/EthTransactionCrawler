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

        //SAMO JEDNA STRANA NOVO 
        public Task<IReadOnlyList<TxDto>> GetTransactionsPageAsync(string address, long startBlock, int page, int pageSize)
    => FetchOneAsync(() => _client.GetTransactionsAsync(address, startBlock, page, pageSize));

        public Task<IReadOnlyList<InternalDto>> GetInternalPageAsync(string address, long startBlock, int page, int pageSize)
            => FetchOneAsync(() => _client.GetInternalTransactionsAsync(address, startBlock, page, pageSize));

        public Task<IReadOnlyList<TokenDto>> GetTokenTransfersPageAsync(string address, long startBlock, int page, int pageSize)
            => FetchOneAsync(() => _client.GetTokenTransfersAsync(address, startBlock, page, pageSize));

        private static async Task<IReadOnlyList<T>> FetchOneAsync<T>(Func<Task<EtherscanResponse<List<T>>>> fetch)
        {
            var resp = await fetch();

            if (resp.status == "0" &&
                (resp.message?.Contains("No transactions", StringComparison.OrdinalIgnoreCase) == true ||
                 resp.message?.Contains("No records", StringComparison.OrdinalIgnoreCase) == true))
                return Array.Empty<T>();

            if (resp.status != "1")
                throw new InvalidOperationException($"Etherscan error: {resp.message}");

            return resp.result ?? new List<T>();
        }

        // Production: NEMA maxPages limita (povlači sve dok ima stranica)
        public Task<IReadOnlyList<TxDto>> GetAllTransactionsAsync(
            string address, long startBlock, int pageSize)
            => FetchAllAsync(
                page => _client.GetTransactionsAsync(address, startBlock, page, pageSize),
                pageSize,
                delayMs: _opt.DelayMsBetweenPages > 0 ? _opt.DelayMsBetweenPages : 1200);

        public Task<IReadOnlyList<InternalDto>> GetAllInternalTransactionsAsync(
            string address, long startBlock, int pageSize)
            => FetchAllAsync(
                page => _client.GetInternalTransactionsAsync(address, startBlock, page, pageSize),
                pageSize,
                delayMs: _opt.DelayMsBetweenPages > 0 ? _opt.DelayMsBetweenPages : 1200);

        public Task<IReadOnlyList<TokenDto>> GetAllTokenTransfersAsync(
            string address, long startBlock, int pageSize)
            => FetchAllAsync(
                page => _client.GetTokenTransfersAsync(address, startBlock, page, pageSize),
                pageSize,
                delayMs: _opt.DelayMsBetweenPages > 0 ? _opt.DelayMsBetweenPages : 1200);

        // ----------------- Generic paginator helper -----------------
        public async Task<IReadOnlyList<T>> FetchAllAsync<T>(
            Func<int, Task<EtherscanResponse<List<T>>>> fetchPage,
            int pageSize,
            int delayMs = 1200,
            int? maxPages = null,                   // i dalje postoji opcija za test, ali je ne koristimo
            CancellationToken ct = default)
        {
            var all = new List<T>();
            var page = 1;

            // simple retry (za rate-limit/sporadične 5xx); ne komplikujemo sa Polly-jem
            const int maxRetries = 3;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                EtherscanResponse<List<T>> resp;
                var attempt = 0;

                while (true)
                {
                    attempt++;
                    resp = await fetchPage(page);

                    // Prazno: status "0" + poruka "No transactions"/"No records"
                    if (resp.status == "0" &&
                        (resp.message?.IndexOf("No transactions", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         resp.message?.IndexOf("No records", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        return all;
                    }

                    // Rate limit/privremene greške: probaj opet par puta
                    var transient =
                        resp.status == "0" &&
                        (resp.message?.IndexOf("rate limit", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         resp.message?.IndexOf("Max rate limit reached", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         resp.message?.IndexOf("Max calls", StringComparison.OrdinalIgnoreCase) >= 0);

                    if (transient && attempt < maxRetries)
                    {
                        await Task.Delay(Math.Max(delayMs, 1000) * attempt, ct);
                        continue;
                    }

                    // Bilo šta drugo što nije uspeh: baci grešku
                    if (resp.status != "1")
                        throw new InvalidOperationException($"Etherscan error: {resp.message}");

                    break; // ok odgovor
                }

                var chunk = resp.result ?? new List<T>();
                if (chunk.Count == 0)
                    return all;

                all.AddRange(chunk);

                // manje od pageSize => nema sledeće stranice
                if (chunk.Count < pageSize)
                    return all;

                // opciono tvrdo ograničenje stranica za test
                if (maxPages.HasValue && page >= maxPages.Value)
                    return all;

                page++;

                // throttle (<= ~1 req/s za free plan)
                if (delayMs > 0)
                    await Task.Delay(delayMs, ct);
            }
        }
    }
}
