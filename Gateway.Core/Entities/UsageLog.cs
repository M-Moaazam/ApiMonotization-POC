namespace Gateway.Core.Entities;

public class UsageLog
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int? UserId { get; set; }   // optional (in case you extend to multiple users per customer)

    public string Endpoint { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int ResponseStatus { get; set; }
    public int LatencyMs { get; set; }
}
