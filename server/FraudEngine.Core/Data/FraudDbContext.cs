using Microsoft.EntityFrameworkCore;
using FraudEngine.Core.Models;

namespace FraudEngine.Core.Data
{
    public class FraudDbContext : DbContext
    {
        public FraudDbContext(DbContextOptions<FraudDbContext> options) : base(options)
        {
        }

        public DbSet<TransactionEvent> Transactions { get; set; }
        public DbSet<FraudAlert> Alerts { get; set; }
        public DbSet<Account> Accounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TransactionEvent>(b =>
            {
                b.HasKey(t => t.Id);
                b.HasIndex(t => t.AccountId);
                b.HasIndex(t => t.MerchantId);
                b.HasIndex(t => t.Channel);
                b.HasIndex(t => t.CountryCode);
                b.Property(t => t.Amount).HasPrecision(18, 2);
            });

            modelBuilder.Entity<FraudAlert>(b =>
            {
                b.HasKey(a => a.Id);
                b.HasIndex(a => a.TransactionId);
            });

            modelBuilder.Entity<Account>(b =>
            {
                b.HasKey(a => a.AccountId);
                b.HasIndex(a => a.AccountId);
            });
        }
    }
}
