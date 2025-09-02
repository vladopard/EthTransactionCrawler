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

        public async Task CrawlAddressAsync(string address)
        {
            //Нормализација адресе
            var addr = NormalizeAddress(address);

            var lastKnown = await GetLastKnownBlockForAddressAsync(addr);
            var startBlock = Math.Max(lastKnown + 1, _defaultStartBlock);

            _logger.LogInformation("Crawling {Address} from block {StartBlock}", addr, startBlock);

            var txTask = _paginator.GetAllTransactionsAsync(addr, startBlock, _pageSize);
            var internalTask = _paginator.GetAllInternalTransactionsAsync(addr, startBlock, _pageSize);
            var tokenTask = _paginator.GetAllTokenTransfersAsync(addr, startBlock, _pageSize);
            await Task.WhenAll(txTask, internalTask, tokenTask);

            var txDtos = await txTask;
            var internalDtos = await internalTask;
            var tokenDtos = await tokenTask;

            if (txDtos.Count == 0 && internalDtos.Count == 0 && tokenDtos.Count == 0)
            {
                _logger.LogInformation("No new data for {Address}", addr);
                return;
            }

            // mapiranje
            var txEntities = _mapper.Map<List<EthTransaction>>(txDtos);
            var internalEntities = _mapper.Map<List<InternalTransaction>>(internalDtos);
            var tokenEntities = _mapper.Map<List<TokenTransfer>>(tokenDtos);

            // normalizuj adrese (lower), null-safe za To
            Normalize(txEntities);
            Normalize(internalEntities);
            Normalize(tokenEntities);

            // dedup (za slučaj overlap-a stranica ili retry)
            txEntities = txEntities.DistinctBy(e => e.Hash).ToList();
            internalEntities = internalEntities.DistinctBy(e => e.UniqueId).ToList();
            tokenEntities = tokenEntities.DistinctBy(e => e.UniqueId).ToList();

            // bulk upsert – obavezno setuj ključ po kom se updejtuje
            var txCfg = new BulkConfig { UpdateByProperties = new List<string> { "Hash" }, BatchSize = 10_000 };
            var inCfg = new BulkConfig { UpdateByProperties = new List<string> { "UniqueId" }, BatchSize = 10_000 };
            var tkCfg = new BulkConfig { UpdateByProperties = new List<string> { "UniqueId" }, BatchSize = 10_000 };

            using var dbTx = await _db.Database.BeginTransactionAsync();
            await _db.BulkInsertOrUpdateAsync(txEntities, txCfg);
            await _db.BulkInsertOrUpdateAsync(internalEntities, inCfg);
            await _db.BulkInsertOrUpdateAsync(tokenEntities, tkCfg);
            await dbTx.CommitAsync();

            _logger.LogInformation("Saved {Tx} tx, {Int} internal, {Tok} token transfers for {Address}",
                txEntities.Count, internalEntities.Count, tokenEntities.Count, addr);

        }

        private static string NormalizeAddress(string? s)
            => s?.Trim().ToLowerInvariant() ?? string.Empty;


        private async Task<long> GetLastKnownBlockForAddressAsync(string addressLower)
        {
            var maxTx = await _db.EthTransactions.AsNoTracking()
            .Where(t => t.From == addressLower || (t.To != null && t.To == addressLower))
            .MaxAsync(t => (long?)t.BlockNumber) ?? 0;

            var maxInt = await _db.InternalTransactions.AsNoTracking()
                .Where(t => t.From == addressLower || (t.To != null && t.To == addressLower))
                .MaxAsync(t => (long?)t.BlockNumber) ?? 0;

            var maxTok = await _db.TokenTransfers.AsNoTracking()
                .Where(t => t.From == addressLower || t.To == addressLower)
                .MaxAsync(t => (long?)t.BlockNumber) ?? 0;

            return Math.Max(maxTx, Math.Max(maxInt, maxTok));
        }
        //ова метода се позове пре него што снимаш у базу,
        //и све адресе у листи трансакција се доведу у "стандардни" формат.
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
