using EthCrawlerApi.Infrastructure;
using EthCrawlerApi.Mapping;
using EthCrawlerApi.Options;
using EthCrawlerApi.Providers.Etherscan;
using EthCrawlerApi.Providers.Etherscan.Interfaces;
using EthCrawlerApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<EthCrawlerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

//This is the advanced/explicit form (the “options builder” API).
builder.Services.AddOptions<EtherscanOptions>()
    .Bind(builder.Configuration.GetSection(EtherscanOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey), "Api key is required!")
    .ValidateOnStart();
//It just binds your configuration section ("Crawler") to a POCO (CrawlerOptions).
builder.Services.Configure<CrawlerOptions>(
    builder.Configuration.GetSection("Crawler"));

builder.Services.AddScoped<IEtherscanPaginator, EtherscanPaginator>();
builder.Services.AddScoped<CrawlerService>();
builder.Services.AddHostedService<CrawlerBackgroundService>();
builder.Services.AddHttpClient<IEtherscanClient, EtherscanClient>((sp, client) =>
{
    var opt = sp.GetRequiredService<IOptions<EtherscanOptions>>().Value;

    client.BaseAddress = new Uri(opt.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(opt.TimeoutSeconds);
});

builder.Services.AddAutoMapper(typeof(EtherscanMappingProfile));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
