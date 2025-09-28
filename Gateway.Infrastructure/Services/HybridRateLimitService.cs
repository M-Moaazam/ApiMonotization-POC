using Gateway.Core.Entities;
using Gateway.Infrastructure.Services;

public class HybridRateLimitService : IRateLimitService
{
    private readonly MemoryRateLimitService _memory;
    private readonly RedisRateLimitService _redis;

    public HybridRateLimitService(MemoryRateLimitService memory, RedisRateLimitService redis)
    {
        _memory = memory;
        _redis = redis;
    }

    public async Task<(bool Allowed, string? Reason, int? RetryAfter)> CheckRequestAsync(int customerId, Tier tier)
    {
        // First increment/check in memory
        var memResult = await _memory.CheckRequestAsync(customerId, tier);
        if (!memResult.Allowed)
        {
            return memResult;
        }

        // Then increment/check in Redis
        return await _redis.CheckRequestAsync(customerId, tier);
    }
}
