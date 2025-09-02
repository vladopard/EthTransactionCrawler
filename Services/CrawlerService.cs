using AutoMapper;
using EthCrawlerApi.Domain;
using EthCrawlerApi.Infrastructure;
using EthCrawlerApi.Options;
using EthCrawlerApi.Providers.Etherscan.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using EFCore.BulkExtensions;

namespace EthCrawlerApi.Services
{

    public class CrawlerService
    {
        private readonly IEtherscanPaginator _paginator;
        private readonly EthCrawlerDbContext _db;
        private readonly IMapper _mapper;
        private readonly ILogger<CrawlerService> _logger;
        private readonly int _pageSize;
        private readonly long _defaultStartBlock;

        public CrawlerService(
            IEtherscanPaginator paginator,
            EthCrawlerDbContext db,
            IMapper mapper,
            ILogger<CrawlerService> logger,
            IOptions<EtherscanOptions> etherscanOptions
        )
        {
            _paginator = paginator;
            _db = db;
            _mapper = mapper;
            _logger = logger;
            _pageSize = etherscanOptions.Value.PageSize;
            _defaultStartBlock = etherscanOptions.Value.DefaultStartBlock;
        }

        public async Task CrawlAddressAsync(string address, CrawlTypes types = CrawlTypes.All)
        {
            var addr = NormalizeAddress(address);

            // per-type start blokovi
            long startTx = _defaultStartBlock, startInt = _defaultStartBlock, startTok = _defaultStartBlock;

            if (types.HasFlag(CrawlTypes.Transactions))
            {
                var lastTx = await GetLastKnownBlockForTransactionsAsync(addr);
                startTx = Math.Max(lastTx + 1, _defaultStartBlock);
                _logger.LogInformation("Crawling TX for {Address} from {Start}", addr, startTx);
            }

            if (types.HasFlag(CrawlTypes.Internal))
            {
                var lastInt = await GetLastKnownBlockForInternalAsync(addr);
                startInt = Math.Max(lastInt + 1, _defaultStartBlock);
                _logger.LogInformation("Crawling INTERNAL for {Address} from {Start}", addr, startInt);
            }

            if (types.HasFlag(CrawlTypes.TokenTransfers))
            {
                var lastTok = await GetLastKnownBlockForTokensAsync(addr);
                startTok = Math.Max(lastTok + 1, _defaultStartBlock);
                _logger.LogInformation("Crawling TOKENS for {Address} from {Start}", addr, startTok);
            }

            var txEntities = new List<EthTransaction>();
            var internalEntities = new List<InternalTransaction>();
            var tokenEntities = new List<TokenTransfer>();

            if (types.HasFlag(CrawlTypes.Transactions))
            {
                var txDtos = await _paginator.GetAllTransactionsAsync(addr, startTx, _pageSize);
                txEntities = _mapper.Map<List<EthTransaction>>(txDtos);
                Normalize(txEntities);
                txEntities = txEntities.DistinctBy(e => e.Hash).ToList();
            }

            if (types.HasFlag(CrawlTypes.Internal))
            {
                if (txEntities.Count > 0) await Task.Delay(1200); // throttle između različitih tipova
                var internalDtos = await _paginator.GetAllInternalTransactionsAsync(addr, startInt, _pageSize);
                internalEntities = _mapper.Map<List<InternalTransaction>>(internalDtos);
                Normalize(internalEntities);
                internalEntities = internalEntities.DistinctBy(e => e.UniqueId).ToList();
            }

            if (types.HasFlag(CrawlTypes.TokenTransfers))
            {
                if (txEntities.Count > 0 || internalEntities.Count > 0) await Task.Delay(1200);
                var tokenDtos = await _paginator.GetAllTokenTransfersAsync(addr, startTok, _pageSize);
                tokenEntities = _mapper.Map<List<TokenTransfer>>(tokenDtos);
                Normalize(tokenEntities);
                tokenEntities = tokenEntities.DistinctBy(e => e.UniqueId).ToList();
            }

            if (txEntities.Count == 0 && internalEntities.Count == 0 && tokenEntities.Count == 0)
            {
                _logger.LogInformation("Nothing to save for {Address}", addr);
                return;
            }

            var txCfg = new BulkConfig { UpdateByProperties = new List<string> { "Hash" }, BatchSize = 10_000 };
            var inCfg = new BulkConfig { UpdateByProperties = new List<string> { "UniqueId" }, BatchSize = 10_000 };
            var tkCfg = new BulkConfig { UpdateByProperties = new List<string> { "UniqueId" }, BatchSize = 10_000 };

            using var dbTx = await _db.Database.BeginTransactionAsync();

            if (txEntities.Count > 0)
                await _db.BulkInsertOrUpdateAsync(txEntities, txCfg);

            if (internalEntities.Count > 0)
                await _db.BulkInsertOrUpdateAsync(internalEntities, inCfg);

            if (tokenEntities.Count > 0)
                await _db.BulkInsertOrUpdateAsync(tokenEntities, tkCfg);

            await dbTx.CommitAsync();

            _logger.LogInformation(
                "Saved {Tx} tx, {Int} internal, {Tok} token transfers for {Address}",
                txEntities.Count, internalEntities.Count, tokenEntities.Count, addr);
        }

        private static string NormalizeAddress(string? s)
            => s?.Trim().ToLowerInvariant() ?? string.Empty;

        private async Task<long> GetLastKnownBlockForTransactionsAsync(string addressLower)
        {
            return await _db.EthTransactions.AsNoTracking()
                .Where(t => t.From == addressLower || (t.To != null && t.To == addressLower))
                .MaxAsync(t => (long?)t.BlockNumber) ?? 0;
        }

        private async Task<long> GetLastKnownBlockForInternalAsync(string addressLower)
        {
            return await _db.InternalTransactions.AsNoTracking()
                .Where(t => t.From == addressLower || (t.To != null && t.To == addressLower))
                .MaxAsync(t => (long?)t.BlockNumber) ?? 0;
        }

        private async Task<long> GetLastKnownBlockForTokensAsync(string addressLower)
        {
            return await _db.TokenTransfers.AsNoTracking()
                .Where(t => t.From == addressLower || t.To == addressLower)
                .MaxAsync(t => (long?)t.BlockNumber) ?? 0;
        }

        // нормализација ентитета
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
