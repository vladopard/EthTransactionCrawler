using EthCrawlerApi.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EthCrawlerApi.Services
{
    public class CrawlerBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<CrawlerBackgroundService> _logger;
        private readonly CrawlerOptions _options;

        public CrawlerBackgroundService(
            IServiceProvider services,
            IOptions<CrawlerOptions> options,
            ILogger<CrawlerBackgroundService> logger)
        {
            _services = services;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // kratko odloži start
            try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
            catch (OperationCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Crawler tick at {Time}", DateTimeOffset.UtcNow);

                foreach (var raw in _options.Addresses.Where(a => !string.IsNullOrWhiteSpace(a)))
                {
                    try
                    {
                        using var scope = _services.CreateScope();
                        var crawler = scope.ServiceProvider.GetRequiredService<CrawlerService>();

                        // Bez prosleđivanja CT-a (namerno)
                        await crawler.CrawlAddressAsync(raw);
                    }
                    catch (OperationCanceledException)
                    {
                        // Dozvoli čist izlaz na shutdown signal
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Crawler error for {Address}", raw);
                    }
                }

                // čekanje do sledećeg kruga (jednom po tick-u, a ne po adresi)
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(_options.CrawlIntervalMinutes), stoppingToken);
                }
                catch (OperationCanceledException) { /* shutdown */ }
            }
        }
    }
}
