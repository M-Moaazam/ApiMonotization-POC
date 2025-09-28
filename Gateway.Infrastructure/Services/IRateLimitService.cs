using Gateway.Core.Entities;

namespace Gateway.Infrastructure.Services
{
    public interface IRateLimitService
    {
        /// <summary>
        /// Checks whether the request is allowed for the given customer and tier.
        /// Returns (allowed, reason, retryAfterSeconds)
        /// </summary>
        Task<(bool Allowed, string? Reason, int? RetryAfter)> CheckRequestAsync(int customerId, Tier tier);
    }
}
