using EthCrawlerApi.Infrastructure;
using EthCrawlerApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EthCrawlerApi.Controllers;

[ApiController]
[Route("api/addresses/{address}")]
public class AddressHistoryController : ControllerBase
{
    private readonly EthCrawlerDbContext _db;
    private readonly CrawlerService _crawler;

    public AddressHistoryController(EthCrawlerDbContext db, CrawlerService crawler)
    {
        _db = db;
        _crawler = crawler;
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions(
        string address,
        long? fromBlock,
        long? toBlock,
        int page = 1,
        int pageSize = 50)
    {
        var addr = address.Trim().ToLowerInvariant();
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 1000);

        var query = _db.EthTransactions.AsNoTracking()
            .Where(t => t.From == addr || (t.To != null && t.To == addr));

        if (fromBlock.HasValue) query = query.Where(t => t.BlockNumber >= fromBlock.Value);
        if (toBlock.HasValue) query = query.Where(t => t.BlockNumber <= toBlock.Value);

        var total = await query.CountAsync();

        if (total == 0)
        {
            await _crawler.CrawlAddressAsync(addr, CrawlTypes.Transactions);

            query = _db.EthTransactions.AsNoTracking()
                .Where(t => t.From == addr || (t.To != null && t.To == addr));

            if (fromBlock.HasValue) query = query.Where(t => t.BlockNumber >= fromBlock.Value);
            if (toBlock.HasValue) query = query.Where(t => t.BlockNumber <= toBlock.Value);

            total = await query.CountAsync();
        }

        var items = await query
            .OrderByDescending(t => t.BlockNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("internal-transactions")]
    public async Task<IActionResult> GetInternalTransactions(
        string address,
        long? fromBlock,
        long? toBlock,
        int page = 1,
        int pageSize = 50)
    {
        var addr = address.Trim().ToLowerInvariant();
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 1000);

        var query = _db.InternalTransactions.AsNoTracking()
            .Where(t => t.From == addr || (t.To != null && t.To == addr));

        if (fromBlock.HasValue) query = query.Where(t => t.BlockNumber >= fromBlock.Value);
        if (toBlock.HasValue) query = query.Where(t => t.BlockNumber <= toBlock.Value);

        var total = await query.CountAsync();

        if (total == 0)
        {
            await _crawler.CrawlAddressAsync(addr, CrawlTypes.Internal);

            query = _db.InternalTransactions.AsNoTracking()
                .Where(t => t.From == addr || (t.To != null && t.To == addr));

            if (fromBlock.HasValue) query = query.Where(t => t.BlockNumber >= fromBlock.Value);
            if (toBlock.HasValue) query = query.Where(t => t.BlockNumber <= toBlock.Value);

            total = await query.CountAsync();
        }

        var items = await query
            .OrderByDescending(t => t.BlockNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("token-transfers")]
    public async Task<IActionResult> GetTokenTransfers(
        string address,
        long? fromBlock,
        long? toBlock,
        int page = 1,
        int pageSize = 50)
    {
        var addr = address.Trim().ToLowerInvariant();
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 1000);

        var query = _db.TokenTransfers.AsNoTracking()
            .Where(t => t.From == addr || t.To == addr);

        if (fromBlock.HasValue) query = query.Where(t => t.BlockNumber >= fromBlock.Value);
        if (toBlock.HasValue) query = query.Where(t => t.BlockNumber <= toBlock.Value);

        var total = await query.CountAsync();

        if (total == 0)
        {
            await _crawler.CrawlAddressAsync(addr, CrawlTypes.TokenTransfers);

            query = _db.TokenTransfers.AsNoTracking()
                .Where(t => t.From == addr || t.To == addr);

            if (fromBlock.HasValue) query = query.Where(t => t.BlockNumber >= fromBlock.Value);
            if (toBlock.HasValue) query = query.Where(t => t.BlockNumber <= toBlock.Value);

            total = await query.CountAsync();
        }

        var items = await query
            .OrderByDescending(t => t.BlockNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }
}
