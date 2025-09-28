using Gateway.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Api.HostedServices
{
    public class MonthlySummaryService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<MonthlySummaryService> _logger;
        private readonly TimeSpan _checkEvery = TimeSpan.FromHours(24); // run daily

        public MonthlySummaryService(IServiceProvider sp, ILogger<MonthlySummaryService> logger)
        {
            _sp = sp;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MonthlySummaryService started");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await GenerateSummariesForPreviousMonth(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while generating monthly summaries");
                }

                await Task.Delay(_checkEvery, stoppingToken);
            }
        }

        private async Task GenerateSummariesForPreviousMonth(CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var start = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
            var end = start.AddMonths(1);

            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApiMonetizationDbContext>();

            var aggregated = await db.UsageLogs
                .Where(u => u.Timestamp >= start && u.Timestamp < end)
                .GroupBy(u => u.CustomerId)
                .Select(g => new { CustomerId = g.Key, TotalRequests = g.Count() })
                .ToListAsync(ct);

            foreach (var item in aggregated)
            {
                var existing = await db.MonthlySummaries
                    .FirstOrDefaultAsync(m => m.CustomerId == item.CustomerId && m.Year == start.Year && m.Month == start.Month, ct);
                if (existing != null)
                {
                    existing.TotalRequests = item.TotalRequests;
                    // optionally recalc AmountDue
                    var customer = await db.Customers.Include(c => c.Tier).FirstOrDefaultAsync(c => c.Id == item.CustomerId, ct);
                    if (customer != null)
                        existing.AmountDue = CalculateAmount(customer.Tier, item.TotalRequests);
                }
                else
                {
                    var customer = await db.Customers.Include(c => c.Tier).FirstOrDefaultAsync(c => c.Id == item.CustomerId, ct);
                    var summary = new Core.Entities.MonthlySummary
                    {
                        CustomerId = item.CustomerId,
                        Year = start.Year,
                        Month = start.Month,
                        TotalRequests = item.TotalRequests,
                        AmountDue = CalculateAmount(customer?.Tier, item.TotalRequests),
                        GeneratedAt = DateTime.UtcNow
                    };
                    db.MonthlySummaries.Add(summary);
                }
            }

            await db.SaveChangesAsync(ct);
        }

        private decimal CalculateAmount(Core.Entities.Tier? tier, int totalRequests)
        {
            if (tier == null) return 0m;
            // simple logic: price per month based on tier; could add extra per-request charges
            return tier.PricePerMonth;
        }
    }
}
