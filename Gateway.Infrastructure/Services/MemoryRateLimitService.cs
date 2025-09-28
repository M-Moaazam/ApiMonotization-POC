using Gateway.Core.Entities;
using Microsoft.Extensions.Caching.Memory;
using System.Runtime.CompilerServices;

namespace Gateway.Infrastructure.Services
{
    public class MemoryRateLimitService
    {
        private readonly IMemoryCache _cache;

        public MemoryRateLimitService(IMemoryCache cache) => _cache = cache;

        public Task<(bool Allowed, string? Reason, int? RetryAfter)> CheckRequestAsync(int customerId, Tier tier)
        {
            var now = DateTime.UtcNow;
            var monthKey = $"quota:{customerId}:{now:yyyyMM}";
            var secKey = $"rate:{customerId}:{now:yyyyMMddHHmmss}";

            lock (GetLockForKey(monthKey))
            {
                var current = _cache.Get<long?>(monthKey) ?? 0L;
                current++;
                _cache.Set(monthKey, current, new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = new DateTimeOffset(new DateTime(now.Year, now.Month, 1).AddMonths(1).AddSeconds(-1))
                });

                if (current > tier.MonthlyQuota)
                {
                    return Task.FromResult((Allowed: false, Reason: (string?)"Monthly quota exceeded", RetryAfter: (int?)null));
                }
            }

            lock (GetLockForKey(secKey))
            {
                var cur = _cache.Get<int?>(secKey) ?? 0;
                cur++;
                _cache.Set(secKey, cur, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2)
                });

                if (cur > tier.RateLimitPerSecond)
                {
                    return Task.FromResult((Allowed: false, Reason: (string?)"Rate limit exceeded", RetryAfter: (int?)1));
                }
            }

            return Task.FromResult((Allowed: true, Reason: (string?)null, RetryAfter: (int?)null));
        }


        // simple per-key lock object
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, object> _locks = new();
        private object GetLockForKey(string key) => _locks.GetOrAdd(key, _ => new object());
    }
}
