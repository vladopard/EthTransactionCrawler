using System.Net.Http.Json;
using System.Text.Json;
using EthCrawlerApi.Options;
using EthCrawlerApi.Providers.Etherscan.Dto;
using EthCrawlerApi.Providers.Etherscan.Interfaces;
using Microsoft.Extensions.Options;

namespace EthCrawlerApi.Providers.Etherscan
{
    public sealed class EtherscanClient : IEtherscanClient
    {
        private readonly HttpClient _http;
        private readonly EtherscanOptions _opt;

        public EtherscanClient(HttpClient http, IOptions<EtherscanOptions> opt)
        {
            _http = http;
            _opt = opt.Value;
        }

        public Task<EtherscanResponse<List<TxDto>>> GetTransactionsAsync(
            string address, long startBlock, int page, int pageSize) =>
            FetchPageAsync<TxDto>("txlist", address, startBlock, page, pageSize);

        public Task<EtherscanResponse<List<InternalDto>>> GetInternalTransactionsAsync(
            string address, long startBlock, int page, int pageSize) =>
            FetchPageAsync<InternalDto>("txlistinternal", address, startBlock, page, pageSize);

        public Task<EtherscanResponse<List<TokenDto>>> GetTokenTransfersAsync(
            string address, long startBlock, int page, int pageSize) =>
            FetchPageAsync<TokenDto>("tokentx", address, startBlock, page, pageSize);

        // ----------------- Helper -----------------
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private async Task<EtherscanResponse<List<T>>> FetchPageAsync<T>(
    string action, string address, long startBlock, int page, int pageSize)
        {
            var url = $"?module=account&action={action}"
                    + $"&address={address}"
                    + $"&startblock={startBlock}"
                    + $"&endblock=99999999"
                    + $"&page={page}"
                    + $"&offset={pageSize}"
                    + $"&sort=asc"
                    + $"&apikey={_opt.ApiKey}";

            var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            resp.EnsureSuccessStatusCode();

            // 1) prvo čitaj wrapper sa result kao JsonElement
            var raw = await resp.Content.ReadFromJsonAsync<EtherscanResponse<JsonElement>>(_jsonOpts);
            if (raw is null)
                throw new InvalidOperationException("Etherscan: empty/invalid JSON response.");

            // 2) “nema podataka” slučaj (status=0 i poruka kaže No transactions)
            if (raw.status == "0" &&
                raw.message?.IndexOf("No transactions", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new EtherscanResponse<List<T>>
                {
                    status = "0",
                    message = raw.message,
                    result = new List<T>()
                };
            }

            // 3) uspeh (status=1) – očekujemo da je result niz
            if (raw.status == "1")
            {
                if (raw.result.ValueKind == JsonValueKind.Array)
                {
                    var list = JsonSerializer.Deserialize<List<T>>(raw.result.GetRawText(), _jsonOpts) ?? new List<T>();
                    return new EtherscanResponse<List<T>>
                    {
                        status = "1",
                        message = raw.message!,
                        result = list
                    };
                }

                // neočekivan format (status=1 ali result nije niz)
                throw new InvalidOperationException("Etherscan: unexpected result format (not an array).");
            }

            // 4) ostale greške (rate limit, invalid key, NOTOK...)
            // često je raw.result string sa porukom
            var details = raw.result.ValueKind == JsonValueKind.String ? raw.result.GetString() : null;
            var msg = string.IsNullOrWhiteSpace(details) ? raw.message : $"{raw.message}: {details}";
            throw new InvalidOperationException($"Etherscan error: {msg}");
        }
    }
}
