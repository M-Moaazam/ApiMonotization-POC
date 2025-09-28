using Gateway.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Infrastructure.Persistence
{
    public class ApiMonetizationDbContext : DbContext
    {
        public ApiMonetizationDbContext(DbContextOptions<ApiMonetizationDbContext> options)
        : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<ApiKey> ApiKeys { get; set; }
        public DbSet<Tier> Tiers { get; set; }
        public DbSet<UsageLog> UsageLogs { get; set; }
        public DbSet<MonthlySummary> MonthlySummaries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.Name)
                .IsUnique(false);

            modelBuilder.Entity<ApiKey>()
                .HasIndex(k => k.Key)
                .IsUnique();

            modelBuilder.Entity<Tier>().HasData(
                new Tier { Id = 1, Name = "Free", MonthlyQuota = 100, RateLimitPerSecond = 2, PricePerMonth = 0m },
                new Tier { Id = 2, Name = "Pro", MonthlyQuota = 100000, RateLimitPerSecond = 10, PricePerMonth = 50m }
            );

            base.OnModelCreating(modelBuilder);
        }
    }
}
