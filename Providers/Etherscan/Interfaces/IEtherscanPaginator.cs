using EthCrawlerApi.Providers.Etherscan.Dto;

namespace EthCrawlerApi.Providers.Etherscan.Interfaces
{
    public interface IEtherscanPaginator
    {
        //Etherscan не враћа све трансакције одједном, већ по страницама.
        //Ендпоинт txlist има параметре: page → број стране(1, 2, 3 …)
        //offset → колико резултата по страни(PageSize)

        //Paginator је сервис који: Врти захтеве по странама(page = 1, 2, 3 …).
        //После сваког позива чека кратко(DelayMsBetweenPages) да не би ударио у rate limit.
        //Стаје када: врати мање резултата од PageSize(значи крај), или
        //врати "No transactions found".
        Task<IReadOnlyList<TxDto>> GetTransactionsPageAsync(string address, long startBlock, int page, int pageSize);
        Task<IReadOnlyList<InternalDto>> GetInternalPageAsync(string address, long startBlock, int page, int pageSize);
        Task<IReadOnlyList<TokenDto>> GetTokenTransfersPageAsync(string address, long startBlock, int page, int pageSize);

        Task<IReadOnlyList<TxDto>> GetAllTransactionsAsync(string address, long startBlock, int pageSize);
        Task<IReadOnlyList<InternalDto>> GetAllInternalTransactionsAsync(string address, long startBlock, int pageSize);
        Task<IReadOnlyList<TokenDto>> GetAllTokenTransfersAsync(string address, long startBlock, int pageSize);

    }
}
