using StackExchange.Redis;
using Gateway.Core.Entities;

namespace Gateway.Infrastructure.Services
{
    public class RedisRateLimitService
    {
        private readonly IConnectionMultiplexer _mux;
        private readonly IDatabase _db;

        public RedisRateLimitService(IConnectionMultiplexer mux)
        {
            _mux = mux;
            _db = _mux.GetDatabase();
        }

        public async Task<(bool Allowed, string? Reason, int? RetryAfter)> CheckRequestAsync(int customerId, Tier tier)
        {
            var now = DateTime.UtcNow;

            // Monthly quota key
            var monthKey = $"quota:{customerId}:{now:yyyyMM}";
            var monthCount = (long)await _db.StringIncrementAsync(monthKey).ConfigureAwait(false);

            // Set TTL until month end on first increment
            if (monthCount == 1)
            {
                var monthEnd = new DateTime(now.Year, now.Month, 1).AddMonths(1).AddSeconds(-1);
                var ttl = monthEnd - now;
                await _db.KeyExpireAsync(monthKey, ttl).ConfigureAwait(false);
            }

            if (monthCount > tier.MonthlyQuota)
            {
                return (false, "Monthly quota exceeded", null);
            }

            // Per-second rate key (window size 1s)
            var secondKey = $"rate:{customerId}:{now:yyyyMMddHHmmss}";
            var secCount = (long)await _db.StringIncrementAsync(secondKey).ConfigureAwait(false);
            if (secCount == 1)
            {
                // expire after 2 seconds to be safe
                await _db.KeyExpireAsync(secondKey, TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            }

            if (secCount > tier.RateLimitPerSecond)
            {
                return (false, "Rate limit exceeded", 1);
            }

            return (true, null, null);
        }
    }
}
