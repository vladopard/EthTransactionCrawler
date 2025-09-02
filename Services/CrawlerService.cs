// File: Services/CrawlerService.cs
using AutoMapper;
using EFCore.BulkExtensions;
using EthCrawlerApi.Domain;
using EthCrawlerApi.Options;
using EthCrawlerApi.Providers.Etherscan.Interfaces;
using EthCrawlerApi.Repositories;
using Microsoft.Extensions.Options;

namespace EthCrawlerApi.Services
{
    public sealed class CrawlerService
    {
        private readonly IEtherscanPaginator _paginator;
        private readonly IEthRepository _repo;
        private readonly IMapper _mapper;
        private readonly ILogger<CrawlerService> _logger;
        private readonly int _pageSizeDefault;
        private readonly long _defaultStartBlock;

        public CrawlerService(
            IEtherscanPaginator paginator,
            IEthRepository repo,
            IMapper mapper,
            ILogger<CrawlerService> logger,
            IOptions<EtherscanOptions> etherscanOptions
        )
        {
            _paginator = paginator;
            _repo = repo;
            _mapper = mapper;
            _logger = logger;
            _pageSizeDefault = etherscanOptions.Value.PageSize;
            _defaultStartBlock = etherscanOptions.Value.DefaultStartBlock;
        }

        // ----------------- PUBLIC API za kontroler -----------------

        //NOVO SINGLE PAGE RETURN
        public async Task<PagedResult<EthTransaction>> GetTransactionsJitAsync(
        string address, long? fromBlock, int page, int pageSize, bool persist = false)
        {
            var addr = NormalizeAddress(address);
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 1000);

            var startBlock = Math.Max(fromBlock ?? _defaultStartBlock, 0);
            var dtos = await _paginator.GetTransactionsPageAsync(addr, startBlock, page, pageSize);
            var entities = _mapper.Map<List<EthTransaction>>(dtos);
            Normalize(entities);
            entities = entities.DistinctBy(e => e.Hash).ToList();

            if (persist && entities.Count > 0)
                await _repo.BulkUpsertTransactionsAsync(entities);

            return new PagedResult<EthTransaction>
            {
                Total = entities.Count,   // or null if you don’t want to guess total
                Page = page,
                PageSize = pageSize,
                Items = entities
            };
        }

        public async Task<PagedResult<InternalTransaction>> GetInternalJitAsync(
            string address, long? fromBlock, int page, int pageSize, bool persist = false)
        {
            var addr = NormalizeAddress(address);
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 1000);

            var startBlock = Math.Max(fromBlock ?? _defaultStartBlock, 0);
            var dtos = await _paginator.GetInternalPageAsync(addr, startBlock, page, pageSize);
            var entities = _mapper.Map<List<InternalTransaction>>(dtos);
            Normalize(entities);
            entities = entities.DistinctBy(e => e.UniqueId).ToList();

            if (persist && entities.Count > 0)
                await _repo.BulkUpsertInternalAsync(entities);

            return new PagedResult<InternalTransaction>
            {
                Total = entities.Count, // nema realan total u JIT režimu
                Page = page,
                PageSize = pageSize,
                Items = entities
            };
        }

        public async Task<PagedResult<TokenTransfer>> GetTokensJitAsync(
            string address, long? fromBlock, int page, int pageSize, bool persist = false)
        {
            var addr = NormalizeAddress(address);
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 1000);

            var startBlock = Math.Max(fromBlock ?? _defaultStartBlock, 0);
            var dtos = await _paginator.GetTokenTransfersPageAsync(addr, startBlock, page, pageSize);
            var entities = _mapper.Map<List<TokenTransfer>>(dtos);
            Normalize(entities);
            entities = entities.DistinctBy(e => e.UniqueId).ToList();

            if (persist && entities.Count > 0)
                await _repo.BulkUpsertTokensAsync(entities);

            return new PagedResult<TokenTransfer>
            {
                Total = entities.Count, // nema realan total u JIT režimu
                Page = page,
                PageSize = pageSize,
                Items = entities
            };
        }


        public async Task<PagedResult<EthTransaction>> GetTransactionsAsync(string address, long? fromBlock, long? toBlock, int page, int pageSize)
        {
            var addr = NormalizeAddress(address);
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 1000);

            var total = await _repo.CountTransactionsAsync(addr, fromBlock, toBlock);
            if (total == 0)
            {
                await CrawlAddressAsync(addr, CrawlTypes.Transactions);
                total = await _repo.CountTransactionsAsync(addr, fromBlock, toBlock);
            }

            var items = await _repo.GetTransactionsPageAsync(addr, fromBlock, toBlock, page, pageSize);
            return new PagedResult<EthTransaction> { Total = total, Page = page, PageSize = pageSize, Items = items };
        }

        public async Task<PagedResult<InternalTransaction>> GetInternalAsync(string address, long? fromBlock, long? toBlock, int page, int pageSize)
        {
            var addr = NormalizeAddress(address);
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 1000);

            var total = await _repo.CountInternalAsync(addr, fromBlock, toBlock);
            if (total == 0)
            {
                await CrawlAddressAsync(addr, CrawlTypes.Internal);
                total = await _repo.CountInternalAsync(addr, fromBlock, toBlock);
            }

            var items = await _repo.GetInternalPageAsync(addr, fromBlock, toBlock, page, pageSize);
            return new PagedResult<InternalTransaction> { Total = total, Page = page, PageSize = pageSize, Items = items };
        }

        public async Task<PagedResult<TokenTransfer>> GetTokensAsync(string address, long? fromBlock, long? toBlock, int page, int pageSize)
        {
            var addr = NormalizeAddress(address);
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 1000);

            var total = await _repo.CountTokensAsync(addr, fromBlock, toBlock);
            if (total == 0)
            {
                await CrawlAddressAsync(addr, CrawlTypes.TokenTransfers);
                total = await _repo.CountTokensAsync(addr, fromBlock, toBlock);
            }

            var items = await _repo.GetTokensPageAsync(addr, fromBlock, toBlock, page, pageSize);
            return new PagedResult<TokenTransfer> { Total = total, Page = page, PageSize = pageSize, Items = items };
        }

        // ----------------- Crawl orchestration -----------------

        public async Task CrawlAddressAsync(string address, CrawlTypes types)
        {
            var lastKnown = Math.Max(
                await _repo.GetMaxTxBlockAsync(address),
                Math.Max(await _repo.GetMaxInternalBlockAsync(address),
                         await _repo.GetMaxTokenBlockAsync(address))
            );

            var startBlock = Math.Max(lastKnown + 1, _defaultStartBlock);

            if (types.HasFlag(CrawlTypes.Transactions))
            {
                _logger.LogInformation("Crawling TX for {Address} from {Start}", address, startBlock);
                var txDtos = await _paginator.GetAllTransactionsAsync(address, startBlock, _pageSizeDefault);
                var txEntities = _mapper.Map<List<EthTransaction>>(txDtos);
                Normalize(txEntities);
                txEntities = txEntities.DistinctBy(e => e.Hash).ToList();
                await _repo.BulkUpsertTransactionsAsync(txEntities);
            }

            if (types.HasFlag(CrawlTypes.Internal))
            {
                _logger.LogInformation("Crawling INTERNAL for {Address} from {Start}", address, startBlock);
                var intDtos = await _paginator.GetAllInternalTransactionsAsync(address, startBlock, _pageSizeDefault);
                var intEntities = _mapper.Map<List<InternalTransaction>>(intDtos);
                Normalize(intEntities);
                intEntities = intEntities.DistinctBy(e => e.UniqueId).ToList();
                await _repo.BulkUpsertInternalAsync(intEntities);
            }

            if (types.HasFlag(CrawlTypes.TokenTransfers))
            {
                _logger.LogInformation("Crawling TOKENS for {Address} from {Start}", address, startBlock);
                var tokDtos = await _paginator.GetAllTokenTransfersAsync(address, startBlock, _pageSizeDefault);
                var tokEntities = _mapper.Map<List<TokenTransfer>>(tokDtos);
                Normalize(tokEntities);
                tokEntities = tokEntities.DistinctBy(e => e.UniqueId).ToList();
                await _repo.BulkUpsertTokensAsync(tokEntities);
            }
        }

        // ----------------- Helpers -----------------
        private static string NormalizeAddress(string? s)
            => s?.Trim().ToLowerInvariant() ?? string.Empty;

        private static void Normalize(List<EthTransaction> list)
        {
            foreach (var e in list)
            {
                e.From = NormalizeAddress(e.From);
                if (!string.IsNullOrWhiteSpace(e.To)) e.To = NormalizeAddress(e.To!);
            }
        }

        private static void Normalize(List<InternalTransaction> list)
        {
            foreach (var e in list)
            {
                e.From = NormalizeAddress(e.From);
                if (!string.IsNullOrWhiteSpace(e.To)) e.To = NormalizeAddress(e.To!);
            }
        }

        private static void Normalize(List<TokenTransfer> list)
        {
            foreach (var e in list)
            {
                e.From = NormalizeAddress(e.From);
                e.To = NormalizeAddress(e.To);
                e.ContractAddress = NormalizeAddress(e.ContractAddress);
            }
        }
    }
}
