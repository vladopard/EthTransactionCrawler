using EthCrawlerApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace EthCrawlerApi.Infrastructure
{
    public class EthCrawlerDbContext : DbContext
    {
        public EthCrawlerDbContext(DbContextOptions<EthCrawlerDbContext> options) : base(options) { }

        public DbSet<EthTransaction> EthTransactions => Set<EthTransaction>();
        public DbSet<InternalTransaction> InternalTransactions => Set<InternalTransaction>();
        public DbSet<TokenTransfer> TokenTransfers => Set<TokenTransfer>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.HasDefaultSchema("public");

            // ---------- EthTransaction ----------
            b.Entity<EthTransaction>(e =>
            {
                e.HasKey(x => x.Hash);

                e.Property(x => x.Hash)
                    .IsRequired()
                    .HasMaxLength(66); // 0x + 64 hex

                e.Property(x => x.BlockNumber).IsRequired();

                e.Property(x => x.TimeStampUtc)
                    .IsRequired();

                e.Property(x => x.From)
                    .IsRequired()
                    .HasMaxLength(64);

                e.Property(x => x.To)
                    .IsRequired()
                    .HasMaxLength(64);

                e.Property(x => x.ValueEth)
                    .IsRequired()
                    .HasPrecision(38, 18); // numeric(38,18)

                e.Property(x => x.GasUsed)
                    .IsRequired()
                    .HasPrecision(38, 18);

                e.Property(x => x.GasPriceGwei)
                    .IsRequired()
                    .HasPrecision(38, 18);

                e.Property(x => x.isError)
                    .IsRequired();

                // Индекси ради брзине упита
                e.HasIndex(x => x.BlockNumber);
                e.HasIndex(x => x.TimeStampUtc);
                e.HasIndex(x => x.From);
                e.HasIndex(x => x.To);
            });

            // ---------- InternalTransaction ----------
            b.Entity<InternalTransaction>(e =>
            {
                // Primarni ključ – UniqueId (TxHash + TraceId)
                e.HasKey(x => x.UniqueId);

                e.Property(x => x.UniqueId)
                    .IsRequired()
                    .HasMaxLength(120);

                e.Property(x => x.Hash)
                    .IsRequired()
                    .HasMaxLength(66);

                e.Property(x => x.BlockNumber).IsRequired();

                e.Property(x => x.TimestampUtc).IsRequired();

                e.Property(x => x.From)
                    .IsRequired()
                    .HasMaxLength(64);

                e.Property(x => x.To)
                    .IsRequired()
                    .HasMaxLength(64);

                e.Property(x => x.ValueEth)
                    .IsRequired()
                    .HasPrecision(38, 18);

                // Индекси
                e.HasIndex(x => x.Hash);
                e.HasIndex(x => x.BlockNumber);
                e.HasIndex(x => x.From);
                e.HasIndex(x => x.To);
            });

            // ---------- TokenTransfer ----------
            b.Entity<TokenTransfer>(e =>
            {
                // Primarni ključ – UniqueId (TxHash + LogIndex)
                e.HasKey(x => x.UniqueId);

                e.Property(x => x.UniqueId)
                    .IsRequired()
                    .HasMaxLength(120);

                e.Property(x => x.TxHash)
                    .IsRequired()
                    .HasMaxLength(66);

                e.Property(x => x.BlockNumber).IsRequired();

                e.Property(x => x.TimestampUtc).IsRequired();

                e.Property(x => x.ContractAddress)
                    .IsRequired()
                    .HasMaxLength(64);

                e.Property(x => x.TokenSymbol)
                    .IsRequired()
                    .HasMaxLength(32);

                e.Property(x => x.TokenDecimals).IsRequired();

                e.Property(x => x.From)
                    .IsRequired()
                    .HasMaxLength(64);

                e.Property(x => x.To)
                    .IsRequired()
                    .HasMaxLength(64);

                e.Property(x => x.Amount)
                    .IsRequired()
                    .HasPrecision(38, 18);

                // Индекси
                e.HasIndex(x => x.TxHash);
                e.HasIndex(x => x.BlockNumber);
                e.HasIndex(x => x.ContractAddress);
                e.HasIndex(x => x.From);
                e.HasIndex(x => x.To);
                e.HasIndex(x => new { x.ContractAddress, x.TokenSymbol }); // чест упит
            });
        }
    }
}
