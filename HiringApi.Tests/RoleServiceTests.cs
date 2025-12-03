using HiringApi.Models;
using HiringApi.Services;
using HiringApi.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace HiringApi.Tests;

public class RoleServiceTests
{
    private readonly RoleService _roleService;
    private readonly Mock<ILogger<RoleService>> _loggerMock;
    private readonly Mock<IOptions<RoleSettings>> _roleSettingsMock;

    public RoleServiceTests()
    {
        _loggerMock = new Mock<ILogger<RoleService>>();
        _roleSettingsMock = new Mock<IOptions<RoleSettings>>();
        _roleSettingsMock.Setup(x => x.Value).Returns(new RoleSettings { ExpirationMonths = 3 });
        _roleService = new RoleService(_loggerMock.Object, _roleSettingsMock.Object);
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
        var role = _roleService.CreateRole(request, requireApproval: false);

        // Assert
        Assert.NotNull(role);
        Assert.NotEqual(Guid.Empty, role.Id);
        Assert.Equal("Software Engineer", role.Title);
        Assert.Equal("Build amazing software", role.Description);
        Assert.Equal("Engineering", role.Department);
        Assert.Equal("Remote", role.Location);
        Assert.False(role.IsExpired);
        Assert.True(role.IsApproved);
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
        var role = _roleService.CreateRole(request, requireApproval: false);
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
        var role = _roleService.CreateRole(request, requireApproval: false);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.True(role.CreatedAt >= beforeCreation.AddSeconds(-1));
        Assert.True(role.CreatedAt <= afterCreation.AddSeconds(1));
    }

    [Fact]
    public void CreateRole_WithApprovalRequired_ShouldCreateUnapprovedRole()
    {
        // Arrange
        var request = new CreateRoleRequest("Test Role", "Description", "Dept", "Location");

        // Act
        var role = _roleService.CreateRole(request, requireApproval: true);

        // Assert
        Assert.False(role.IsApproved);
    }

    [Fact]
    public void GetAllRoles_ShouldReturnOnlyApprovedRolesByDefault()
    {
        // Arrange
        var approvedRole = new CreateRoleRequest("Approved Role", "Description", "Dept", "Location");
        var unapprovedRole = new CreateRoleRequest("Unapproved Role", "Description", "Dept", "Location");
        _roleService.CreateRole(approvedRole, requireApproval: false);
        _roleService.CreateRole(unapprovedRole, requireApproval: true);

        // Act
        var roles = _roleService.GetAllRoles(includeExpired: false, includeUnapproved: false);

        // Assert
        Assert.Single(roles);
        Assert.All(roles, role => Assert.True(role.IsApproved));
    }

    [Fact]
    public void GetAllRoles_ShouldReturnAllRolesWhenIncludeUnapprovedIsTrue()
    {
        // Arrange
        var role1 = new CreateRoleRequest("Role 1", "Description 1", "Dept 1", "Location 1");
        var role2 = new CreateRoleRequest("Role 2", "Description 2", "Dept 2", "Location 2");
        _roleService.CreateRole(role1, requireApproval: false);
        _roleService.CreateRole(role2, requireApproval: true);

        // Act
        var roles = _roleService.GetAllRoles(includeExpired: true, includeUnapproved: true);

        // Assert
        Assert.Equal(2, roles.Count());
    }

    [Fact]
    public void GetRoleById_ShouldReturnRoleWhenExists()
    {
        // Arrange
        var request = new CreateRoleRequest("Test Role", "Description", "Dept", "Location");
        var createdRole = _roleService.CreateRole(request, requireApproval: false);

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
    public void ApproveRole_ShouldSetRoleAsApproved()
    {
        // Arrange
        var request = new CreateRoleRequest("Test Role", "Description", "Dept", "Location");
        var role = _roleService.CreateRole(request, requireApproval: true);
        Assert.False(role.IsApproved);

        // Act
        _roleService.ApproveRole(role.Id);

        // Assert
        var approvedRole = _roleService.GetRoleById(role.Id);
        Assert.NotNull(approvedRole);
        Assert.True(approvedRole.IsApproved);
    }

    [Fact]
    public void CreateRole_ShouldGenerateUniqueIdsForMultipleRoles()
    {
        // Arrange
        var request1 = new CreateRoleRequest("Role 1", "Description 1", "Dept 1", "Location 1");
        var request2 = new CreateRoleRequest("Role 2", "Description 2", "Dept 2", "Location 2");

        // Act
        var role1 = _roleService.CreateRole(request1, requireApproval: false);
        var role2 = _roleService.CreateRole(request2, requireApproval: false);

        // Assert
        Assert.NotEqual(role1.Id, role2.Id);
    }

    [Fact]
    public void CreateRole_ShouldLogRoleCreation()
    {
        // Arrange
        var request = new CreateRoleRequest("Test Role", "Description", "Dept", "Location");

        // Act
        var role = _roleService.CreateRole(request, requireApproval: false);

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
