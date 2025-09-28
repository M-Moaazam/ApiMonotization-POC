namespace Gateway.Core.Entities;

public class ApiKey
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
}
