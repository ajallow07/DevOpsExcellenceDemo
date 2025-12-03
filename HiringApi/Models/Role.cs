namespace HiringApi.Models;

public class Role
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsApproved { get; set; } = true;

    public Role()
    {
        ExpiresAt = DateTime.UtcNow.AddMonths(3);
    }
    
    public Role(int expirationMonths)
    {
        ExpiresAt = DateTime.UtcNow.AddMonths(expirationMonths);
    }
}

public record CreateRoleRequest(
    string Title,
    string Description,
    string Department,
    string Location
);
