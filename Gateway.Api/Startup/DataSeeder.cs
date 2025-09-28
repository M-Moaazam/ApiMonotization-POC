using Gateway.Infrastructure.Persistence;
using Gateway.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Api.Startup
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(IServiceProvider sp)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApiMonetizationDbContext>();
            await db.Database.EnsureCreatedAsync();

            if (!db.Tiers.Any())
            {
                db.Tiers.AddRange(new Tier { Name = "Free", MonthlyQuota = 100, RateLimitPerSecond = 2, PricePerMonth = 0m },
                                 new Tier { Name = "Pro", MonthlyQuota = 100000, RateLimitPerSecond = 10, PricePerMonth = 50m });
                await db.SaveChangesAsync();
            }

            if (!db.Customers.Any())
            {
                var tier = db.Tiers.First();
                var customer = new Customer { Name = "Demo Customer", TierId = tier.Id };
                db.Customers.Add(customer);
                await db.SaveChangesAsync();

                var apiKey = new ApiKey { Key = Guid.NewGuid().ToString("N"), CustomerId = customer.Id, IsActive = true, CreatedAt = DateTime.UtcNow };
                db.ApiKeys.Add(apiKey);
                await db.SaveChangesAsync();

                // output key to console for developer
                var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DataSeeder");
                logger.LogInformation("Seeded demo API key: {Key}", apiKey.Key);
            }
        }
    }
}
