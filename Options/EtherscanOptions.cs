using System.ComponentModel.DataAnnotations;

namespace EthCrawlerApi.Options
{
    public sealed class EtherscanOptions
    {
        public const string SectionName = "Etherscan";

        [Required] public string ApiKey { get; init; } = default!;
        public string BaseUrl { get; init; } = "https://api.etherscan.io/api";

        [Range(1, 10_000)]
        public int PageSize { get; init; } = 5;

        [Range(0, 10_000)]
        public int DelayMsBetweenPages { get; init; } = 200;

        [Range(1, 300)]
        public int TimeoutSeconds { get; init; } = 30;

        [Range(0, long.MaxValue)]
        public long DefaultStartBlock { get; init; } = 19_000_000;
    }
}
