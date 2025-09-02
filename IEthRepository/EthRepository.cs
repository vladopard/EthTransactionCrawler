// File: Infrastructure/Repositories/EthRepository.cs
using EthCrawlerApi.Domain;
using Microsoft.EntityFrameworkCore;
using EFCore.BulkExtensions;
using EthCrawlerApi.Infrastructure;


namespace EthCrawlerApi.Repositories
{
    public sealed class EthRepository : IEthRepository
    {
        private readonly EthCrawlerDbContext _db;

        public EthRepository(EthCrawlerDbContext db) => _db = db;

        private static IQueryable<EthTransaction> TxQuery(IQueryable<EthTransaction> q, string addr, long? from, long? to)
        {
            q = q.Where(t => t.From == addr || (t.To != null && t.To == addr));
            if (from.HasValue) q = q.Where(t => t.BlockNumber >= from.Value);
            if (to.HasValue) q = q.Where(t => t.BlockNumber <= to.Value);
            return q;
        }

        private static IQueryable<InternalTransaction> IntQuery(IQueryable<InternalTransaction> q, string addr, long? from, long? to)
        {
            q = q.Where(t => t.From == addr || (t.To != null && t.To == addr));
            if (from.HasValue) q = q.Where(t => t.BlockNumber >= from.Value);
            if (to.HasValue) q = q.Where(t => t.BlockNumber <= to.Value);
            return q;
        }

        private static IQueryable<TokenTransfer> TokQuery(IQueryable<TokenTransfer> q, string addr, long? from, long? to)
        {
            q = q.Where(t => t.From == addr || t.To == addr);
            if (from.HasValue) q = q.Where(t => t.BlockNumber >= from.Value);
            if (to.HasValue) q = q.Where(t => t.BlockNumber <= to.Value);
            return q;
        }

        // -------------------- Transactions --------------------
        public async Task<int> CountTransactionsAsync(string address, long? fromBlock, long? toBlock)
            => await TxQuery(_db.EthTransactions.AsNoTracking(), address, fromBlock, toBlock).CountAsync();

        public async Task<List<EthTransaction>> GetTransactionsPageAsync(string address, long? fromBlock, long? toBlock, int page, int pageSize)
            => await TxQuery(_db.EthTransactions.AsNoTracking(), address, fromBlock, toBlock)
                    .OrderByDescending(t => t.BlockNumber)
                    .Skip((Math.Max(page, 1) - 1) * Math.Clamp(pageSize, 1, 1000))
                    .Take(Math.Clamp(pageSize, 1, 1000))
                    .ToListAsync();

        public async Task<long> GetMaxTxBlockAsync(string address)
            => await _db.EthTransactions.AsNoTracking()
                   .Where(t => t.From == address || (t.To != null && t.To == address))
                   .MaxAsync(t => (long?)t.BlockNumber) ?? 0;

        public async Task BulkUpsertTransactionsAsync(List<EthTransaction> items)
        {
            if (items.Count == 0) return;
            var cfg = new BulkConfig { UpdateByProperties = new() { "Hash" }, BatchSize = 10_000 };
            await _db.BulkInsertOrUpdateAsync(items, cfg);
        }

        // -------------------- Internal --------------------
        public async Task<int> CountInternalAsync(string address, long? fromBlock, long? toBlock)
            => await IntQuery(_db.InternalTransactions.AsNoTracking(), address, fromBlock, toBlock).CountAsync();

        public async Task<List<InternalTransaction>> GetInternalPageAsync(string address, long? fromBlock, long? toBlock, int page, int pageSize)
            => await IntQuery(_db.InternalTransactions.AsNoTracking(), address, fromBlock, toBlock)
                    .OrderByDescending(t => t.BlockNumber)
                    .Skip((Math.Max(page, 1) - 1) * Math.Clamp(pageSize, 1, 1000))
                    .Take(Math.Clamp(pageSize, 1, 1000))
                    .ToListAsync();

        public async Task<long> GetMaxInternalBlockAsync(string address)
            => await _db.InternalTransactions.AsNoTracking()
                   .Where(t => t.From == address || (t.To != null && t.To == address))
                   .MaxAsync(t => (long?)t.BlockNumber) ?? 0;

        public async Task BulkUpsertInternalAsync(List<InternalTransaction> items)
        {
            if (items.Count == 0) return;
            var cfg = new BulkConfig { UpdateByProperties = new() { "UniqueId" }, BatchSize = 10_000 };
            await _db.BulkInsertOrUpdateAsync(items, cfg);
        }

        // -------------------- Token Transfers --------------------
        public async Task<int> CountTokensAsync(string address, long? fromBlock, long? toBlock)
            => await TokQuery(_db.TokenTransfers.AsNoTracking(), address, fromBlock, toBlock).CountAsync();

        public async Task<List<TokenTransfer>> GetTokensPageAsync(string address, long? fromBlock, long? toBlock, int page, int pageSize)
            => await TokQuery(_db.TokenTransfers.AsNoTracking(), address, fromBlock, toBlock)
                    .OrderByDescending(t => t.BlockNumber)
                    .Skip((Math.Max(page, 1) - 1) * Math.Clamp(pageSize, 1, 1000))
                    .Take(Math.Clamp(pageSize, 1, 1000))
                    .ToListAsync();

        public async Task<long> GetMaxTokenBlockAsync(string address)
            => await _db.TokenTransfers.AsNoTracking()
                   .Where(t => t.From == address || t.To == address)
                   .MaxAsync(t => (long?)t.BlockNumber) ?? 0;

        public async Task BulkUpsertTokensAsync(List<TokenTransfer> items)
        {
            if (items.Count == 0) return;
            var cfg = new BulkConfig { UpdateByProperties = new() { "UniqueId" }, BatchSize = 10_000 };
            await _db.BulkInsertOrUpdateAsync(items, cfg);
        }
    }
}
