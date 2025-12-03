using HiringApi.Models;
using HiringApi.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace HiringApi.Services;

/// <summary>
/// Service interface for managing job roles
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Creates a new job role
    /// </summary>
    /// <param name="request">Role creation request</param>
    /// <param name="requireApproval">Whether the role requires approval before being published</param>
    /// <returns>Created role</returns>
    Role CreateRole(CreateRoleRequest request, bool requireApproval);
    
    /// <summary>
    /// Gets all job roles with optional filtering
    /// </summary>
    /// <param name="includeExpired">Include expired roles</param>
    /// <param name="includeUnapproved">Include unapproved roles</param>
    /// <returns>Collection of roles</returns>
    IEnumerable<Role> GetAllRoles(bool includeExpired = false, bool includeUnapproved = false);
    
    /// <summary>
    /// Gets a specific role by ID
    /// </summary>
    /// <param name="id">Role identifier</param>
    /// <returns>Role if found, null otherwise</returns>
    Role? GetRoleById(Guid id);
    
    /// <summary>
    /// Approves a pending role
    /// </summary>
    /// <param name="id">Role identifier</param>
    void ApproveRole(Guid id);
}

/// <summary>
/// Implementation of role management service
/// </summary>
public class RoleService : IRoleService
{
    private readonly ConcurrentDictionary<Guid, Role> _roles = new();
    private readonly ILogger<RoleService> _logger;
    private readonly RoleSettings _roleSettings;

    public RoleService(ILogger<RoleService> logger, IOptions<RoleSettings> roleSettings)
    {
        _logger = logger;
        _roleSettings = roleSettings.Value;
    }

    public Role CreateRole(CreateRoleRequest request, bool requireApproval)
    {
        var role = new Role(_roleSettings.ExpirationMonths)
        {
            Title = request.Title,
            Description = request.Description,
            Department = request.Department,
            Location = request.Location,
            IsApproved = !requireApproval
        };

        _roles.TryAdd(role.Id, role);
        _logger.LogInformation("Role created: {RoleId} - {Title}, Expires: {ExpiresAt}, Approved: {IsApproved}", 
            role.Id, role.Title, role.ExpiresAt, role.IsApproved);
        
        return role;
    }

    public IEnumerable<Role> GetAllRoles(bool includeExpired = false, bool includeUnapproved = false)
    {
        var roles = _roles.Values.AsEnumerable();
        
        if (!includeExpired)
            roles = roles.Where(r => !r.IsExpired);
        
        if (!includeUnapproved)
            roles = roles.Where(r => r.IsApproved);
        
        return roles;
    }

    public Role? GetRoleById(Guid id)
    {
        _roles.TryGetValue(id, out var role);
        return role;
    }

    public void ApproveRole(Guid id)
    {
        if (_roles.TryGetValue(id, out var role))
        {
            role.IsApproved = true;
            _logger.LogInformation("Role approved: {RoleId} - {Title}", role.Id, role.Title);
        }
    }
}
