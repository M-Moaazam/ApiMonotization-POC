using Gateway.Infrastructure.Persistence;
using Gateway.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Net.Http.Headers;
using Xunit;

public class GatewayIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GatewayIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace DB with in-memory
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApiMonetizationDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<ApiMonetizationDbContext>(options => options.UseInMemoryDatabase("TestDb"));

                // force MemoryRateLimit
                var rateDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IRateLimitService));
                if (rateDesc != null) services.Remove(rateDesc);
                services.AddSingleton<IRateLimitService, Gateway.Infrastructure.Services.MemoryRateLimitService>();
            });
        });
    }

    [Fact]
    public async Task Returns_200_For_Valid_Request()
    {
        var client = _factory.CreateClient();

        // Seed API key in in-memory DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApiMonetizationDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var tier = new Gateway.Core.Entities.Tier { Id = 99, Name = "Test", MonthlyQuota = 100, RateLimitPerSecond = 10, PricePerMonth = 0 };
            db.Tiers.Add(tier);
            var customer = new Gateway.Core.Entities.Customer { Id = 1, Name = "Test Cust", TierId = 99, Tier = tier };
            db.Customers.Add(customer);
            var apiKey = new Gateway.Core.Entities.ApiKey { Id = 1, Key = "test-key", CustomerId = 1, Customer = customer, IsActive = true };
            db.ApiKeys.Add(apiKey);
            db.SaveChanges();
        }

        var request = new HttpRequestMessage(HttpMethod.Get, "/health"); // create controller endpoint /health returning 200
        request.Headers.Add("x-api-key", "test-key");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
