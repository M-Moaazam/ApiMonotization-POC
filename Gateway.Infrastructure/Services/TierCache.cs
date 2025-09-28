using Gateway.Core.Entities;
using Gateway.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.Infrastructure.Services
{
    public interface ITierCache
    {
        Task<Tier> GetTierAsync(int tierId, CancellationToken ct = default);
        Task RefreshAsync(CancellationToken ct = default);
    }

    public class TierCache : ITierCache
    {
        private readonly IMemoryCache _cache;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(1);

        private const string KeyPrefix = "tier_";

        public TierCache(IMemoryCache cache, IServiceScopeFactory scopeFactory)
        {
            _cache = cache;
            _scopeFactory = scopeFactory;
        }

        public async Task<Tier> GetTierAsync(int tierId, CancellationToken ct = default)
        {
            var key = KeyPrefix + tierId;
            if (_cache.TryGetValue<Tier>(key, out var tier)) return tier!;

            await RefreshAsync(ct);
            _cache.TryGetValue<Tier>(key, out tier);
            return tier!;
        }

        public async Task RefreshAsync(CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApiMonetizationDbContext>();
            var tiers = await db.Tiers.AsNoTracking().ToListAsync(ct);
            foreach (var t in tiers)
            {
                _cache.Set(KeyPrefix + t.Id, t, DateTimeOffset.UtcNow.Add(_cacheTtl));
            }
        }
    }
}
