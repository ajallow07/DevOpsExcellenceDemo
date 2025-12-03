using HiringApi.Models;
using System.Collections.Concurrent;

namespace HiringApi.Services;

public interface IRoleService
{
    Role CreateRole(CreateRoleRequest request);
    IEnumerable<Role> GetAllRoles(bool includeExpired = false);
    Role? GetRoleById(Guid id);
}

public class RoleService : IRoleService
{
    private readonly ConcurrentDictionary<Guid, Role> _roles = new();
    private readonly ILogger<RoleService> _logger;

    public RoleService(ILogger<RoleService> logger)
    {
        _logger = logger;
    }

    public Role CreateRole(CreateRoleRequest request)
    {
        var role = new Role
        {
            Title = request.Title,
            Description = request.Description,
            Department = request.Department,
            Location = request.Location
        };

        _roles.TryAdd(role.Id, role);
        _logger.LogInformation("Role created: {RoleId} - {Title}, Expires: {ExpiresAt}", 
            role.Id, role.Title, role.ExpiresAt);
        
        return role;
    }

    public IEnumerable<Role> GetAllRoles(bool includeExpired = false)
    {
        var roles = _roles.Values;
        return includeExpired 
            ? roles 
            : roles.Where(r => !r.IsExpired);
    }

    public Role? GetRoleById(Guid id)
    {
        _roles.TryGetValue(id, out var role);
        return role;
    }
}
