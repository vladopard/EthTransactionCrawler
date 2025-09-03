// File: Controllers/AddressHistoryController.cs
using EthCrawlerApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EthCrawlerApi.Controllers;

[ApiController]
[Route("api/addresses/{address}")]
public class AddressHistoryController : ControllerBase
{
    private readonly CrawlerService _service;

    public AddressHistoryController(CrawlerService service)
    {
        _service = service;
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions(
        string address,
        long? fromBlock,
        int page = 1,
        int pageSize = 50,
        bool persist = true)
    {
        var result = await _service.GetTransactionsJitAsync(address, fromBlock, page, pageSize, persist);
        return Ok(result);
    }

    [HttpGet("internal-transactions")]
    public async Task<IActionResult> GetInternalTransactions(
        string address,
        long? fromBlock,
        int page = 1,
        int pageSize = 50,
        bool persist = true)
    {
        var result = await _service.GetInternalJitAsync(address, fromBlock, page, pageSize, persist);
        return Ok(result);
    }

    [HttpGet("token-transfers")]
    public async Task<IActionResult> GetTokenTransfers(
        string address,
        long? fromBlock,
        int page = 1,
        int pageSize = 50,
        bool persist = true)
    {
        var result = await _service.GetTokensJitAsync(address, fromBlock, page, pageSize, persist);
        return Ok(result);
    }
}
