using HiringApi.Models;
using HiringApi.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HiringApi.Tests;

public class RoleServiceTests
{
    private readonly RoleService _roleService;
    private readonly Mock<ILogger<RoleService>> _loggerMock;

    public RoleServiceTests()
    {
        _loggerMock = new Mock<ILogger<RoleService>>();
        _roleService = new RoleService(_loggerMock.Object);
    }

    [Fact]
    public void CreateRole_ShouldCreateRoleWithValidData()
    {
        // Arrange
        var request = new CreateRoleRequest(
            Title: "Software Engineer",
            Description: "Build amazing software",
            Department: "Engineering",
            Location: "Remote"
        );

        // Act
        var role = _roleService.CreateRole(request);

        // Assert
        Assert.NotNull(role);
        Assert.NotEqual(Guid.Empty, role.Id);
        Assert.Equal("Software Engineer", role.Title);
        Assert.Equal("Build amazing software", role.Description);
        Assert.Equal("Engineering", role.Department);
        Assert.Equal("Remote", role.Location);
        Assert.False(role.IsExpired);
    }

    [Fact]
    public void CreateRole_ShouldSetExpirationTo3MonthsFromCreation()
    {
        // Arrange
        var request = new CreateRoleRequest(
            Title: "Product Manager",
            Description: "Lead product strategy",
            Department: "Product",
            Location: "New York"
        );
        var beforeCreation = DateTime.UtcNow;

        // Act
        var role = _roleService.CreateRole(request);
        var afterCreation = DateTime.UtcNow;

        // Assert
        var expectedExpiration = beforeCreation.AddMonths(3);
        Assert.True(role.ExpiresAt >= expectedExpiration.AddSeconds(-1));
        Assert.True(role.ExpiresAt <= afterCreation.AddMonths(3).AddSeconds(1));
    }

    [Fact]
    public void CreateRole_ShouldSetCreatedAtToCurrentTime()
    {
        // Arrange
        var request = new CreateRoleRequest(
            Title: "DevOps Engineer",
            Description: "Manage infrastructure",
            Department: "Operations",
            Location: "Seattle"
        );
        var beforeCreation = DateTime.UtcNow;

        // Act
        var role = _roleService.CreateRole(request);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.True(role.CreatedAt >= beforeCreation.AddSeconds(-1));
        Assert.True(role.CreatedAt <= afterCreation.AddSeconds(1));
    }

    [Fact]
    public void GetAllRoles_ShouldReturnOnlyActiveRolesByDefault()
    {
        // Arrange
        var activeRole = new CreateRoleRequest("Active Role", "Description", "Dept", "Location");
        _roleService.CreateRole(activeRole);

        // Act
        var roles = _roleService.GetAllRoles(includeExpired: false);

        // Assert
        Assert.NotEmpty(roles);
        Assert.All(roles, role => Assert.False(role.IsExpired));
    }

    [Fact]
    public void GetAllRoles_ShouldReturnAllRolesWhenIncludeExpiredIsTrue()
    {
        // Arrange
        var role1 = new CreateRoleRequest("Role 1", "Description 1", "Dept 1", "Location 1");
        var role2 = new CreateRoleRequest("Role 2", "Description 2", "Dept 2", "Location 2");
        _roleService.CreateRole(role1);
        _roleService.CreateRole(role2);

        // Act
        var roles = _roleService.GetAllRoles(includeExpired: true);

        // Assert
        Assert.Equal(2, roles.Count());
    }

    [Fact]
    public void GetRoleById_ShouldReturnRoleWhenExists()
    {
        // Arrange
        var request = new CreateRoleRequest("Test Role", "Description", "Dept", "Location");
        var createdRole = _roleService.CreateRole(request);

        // Act
        var retrievedRole = _roleService.GetRoleById(createdRole.Id);

        // Assert
        Assert.NotNull(retrievedRole);
        Assert.Equal(createdRole.Id, retrievedRole.Id);
        Assert.Equal("Test Role", retrievedRole.Title);
    }

    [Fact]
    public void GetRoleById_ShouldReturnNullWhenRoleDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var role = _roleService.GetRoleById(nonExistentId);

        // Assert
        Assert.Null(role);
    }

    [Fact]
    public void CreateRole_ShouldGenerateUniqueIdsForMultipleRoles()
    {
        // Arrange
        var request1 = new CreateRoleRequest("Role 1", "Description 1", "Dept 1", "Location 1");
        var request2 = new CreateRoleRequest("Role 2", "Description 2", "Dept 2", "Location 2");

        // Act
        var role1 = _roleService.CreateRole(request1);
        var role2 = _roleService.CreateRole(request2);

        // Assert
        Assert.NotEqual(role1.Id, role2.Id);
    }

    [Fact]
    public void CreateRole_ShouldLogRoleCreation()
    {
        // Arrange
        var request = new CreateRoleRequest("Test Role", "Description", "Dept", "Location");

        // Act
        var role = _roleService.CreateRole(request);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Role created")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
