namespace EthCrawlerApi.Services
{
    public sealed class PagedResult<T>
    {
        public required int Total { get; init; }
        public required int Page { get; init; }
        public required int PageSize { get; init; }
        public required List<T> Items { get; init; }

    }
}
