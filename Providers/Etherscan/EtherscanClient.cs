using System.Net.Http.Json;
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
        private async Task<EtherscanResponse<List<T>>> FetchPageAsync<T>(
            string action, string address, long startBlock, int page, int pageSize)
        {
            /* client.BaseAddress = new Uri(opt.BaseUrl);
             * Значи сваки позив ка _http.GetAsync(url) почиње од "https://api.etherscan.io/api".
             * Зато овде састављаш само query string (оно што иде после ?).
             */
            var url = $"?module=account&action={action}" +
                      $"&address={address}" +
                      $"&startblock={startBlock}" +
                      $"&endblock=99999999" +
                      $"&page={page}" +
                      $"&offset={pageSize}" +
                      $"&sort=asc" +
                      $"&apikey={_opt.ApiKey}";

            /* Чим стигну HTTP хедери од сервера (status code, content-length, итд.),
             * HttpClient ти одмах враћа HttpResponseMessage.
             * Тело (body) у том тренутку још није скинуто,
             * већ можеш сам да га читаш као stream 
             * (нпр. await response.Content.ReadAsStreamAsync()).
             */
            var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            // баца HttpRequestException ако није добар статус код (нпр. 404, 500...)
            resp.EnsureSuccessStatusCode();

            //EtherscanResponse<List<T>>?
            var dto = await resp.Content.ReadFromJsonAsync<EtherscanResponse<List<T>>>();
            if (dto is null)
                throw new InvalidOperationException("Etherscan: empty/invalid JSON response.");

            // status 0 може да буде грешка или само нема података
            // Etherscan конвенција: status "0" + message "No transactions found" није грешка
            // то само значи да за дату адресу у том опсегу блокова нема трансакција.
            if (dto.status == "0" &&
                dto.message?.IndexOf("No transactions", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new EtherscanResponse<List<T>>
                {
                    status = "0",
                    message = dto.message,
                    result = new List<T>()
                };
            }

            // Све остало са status != "1" третирамо као грешку Etherscan-а
            if (dto.status != "1")
                throw new InvalidOperationException($"Etherscan error: {dto.message}");

            return dto;
        }
    }
}
