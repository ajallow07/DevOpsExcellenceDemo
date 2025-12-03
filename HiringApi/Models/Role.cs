namespace HiringApi.Models;

/// <summary>
/// Represents a job role with expiration and approval capabilities
/// </summary>
public class Role
{
    /// <summary>
    /// Unique identifier for the role
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Job title
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed job description
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Department name
    /// </summary>
    public string Department { get; set; } = string.Empty;
    
    /// <summary>
    /// Job location
    /// </summary>
    public string Location { get; set; } = string.Empty;
    
    /// <summary>
    /// When the role was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the role expires (UTC)
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// Indicates if the role has expired
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    
    /// <summary>
    /// Indicates if the role has been approved for publishing
    /// </summary>
    public bool IsApproved { get; set; } = true;

    /// <summary>
    /// Creates a new role with default 3-month expiration
    /// </summary>
    public Role()
    {
        ExpiresAt = DateTime.UtcNow.AddMonths(3);
    }
    
    /// <summary>
    /// Creates a new role with custom expiration period
    /// </summary>
    /// <param name="expirationMonths">Number of months until expiration</param>
    public Role(int expirationMonths)
    {
        ExpiresAt = DateTime.UtcNow.AddMonths(expirationMonths);
    }
}

/// <summary>
/// Request model for creating a new job role
/// </summary>
/// <param name="Title">Job title (required)</param>
/// <param name="Description">Detailed job description (required)</param>
/// <param name="Department">Department name</param>
/// <param name="Location">Job location</param>
public record CreateRoleRequest(
    string Title,
    string Description,
    string Department,
    string Location
);
