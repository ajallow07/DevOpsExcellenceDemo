using Microsoft.FeatureManagement;
using HiringApi.Models;
using HiringApi.Services;
using HiringApi.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry();
builder.Logging.AddApplicationInsights();

builder.Services.AddFeatureManagement(builder.Configuration.GetSection("Features"));
builder.Services.AddHealthChecks();
builder.Services.Configure<RoleSettings>(builder.Configuration.GetSection("RoleSettings"));
builder.Services.AddSingleton<IRoleService, RoleService>();

var app = builder.Build();

app.MapHealthChecks("/healthz");

app.MapGet("/api/hiring-status", async (IFeatureManager fm, ILogger<Program> logger) =>
{
    var hired = await fm.IsEnabledAsync("Hired");
    var msg = hired 
        ? "Congratulations! We're thrilled to offer you a position on our team. Welcome aboard!" 
        : "Thank you for your interest in joining our team. While we were impressed with your qualifications, we've decided to move forward with other candidates at this time. We wish you the best in your job search!";
    logger.LogInformation("Hiring status checked. Hired={Hired}", hired);
    return Results.Ok(new { hired, message = msg });
})
.WithName("HiringStatus")
.WithOpenApi();

app.MapPost("/api/roles", async (CreateRoleRequest request, IRoleService roleService, IFeatureManager fm, ILogger<Program> logger) =>
{
    // Check if role posting is enabled
    var rolePostingEnabled = await fm.IsEnabledAsync("EnableRolePosting");
    if (!rolePostingEnabled)
    {
        logger.LogWarning("Role posting attempt blocked - feature disabled");
        return Results.StatusCode(503); // Service Unavailable
    }

    if (string.IsNullOrWhiteSpace(request.Title))
        return Results.BadRequest(new { error = "Title is required" });
    
    if (string.IsNullOrWhiteSpace(request.Description))
        return Results.BadRequest(new { error = "Description is required" });
    
    var requireApproval = await fm.IsEnabledAsync("RequireRoleApproval");
    var role = roleService.CreateRole(request, requireApproval);
    
    logger.LogInformation("New role posted: {RoleId} - {Title}, RequiresApproval: {RequireApproval}", 
        role.Id, role.Title, requireApproval);
    
    return Results.Created($"/api/roles/{role.Id}", role);
})
.WithName("CreateRole")
.WithOpenApi();

app.MapGet("/api/roles", async (IRoleService roleService, IFeatureManager fm, bool includeExpired = false) =>
{
    var showExpired = await fm.IsEnabledAsync("ShowExpiredRoles");
    var roles = roleService.GetAllRoles(includeExpired || showExpired, includeUnapproved: false);
    return Results.Ok(roles);
})
.WithName("GetRoles")
.WithOpenApi();

app.MapGet("/api/roles/{id:guid}", (Guid id, IRoleService roleService) =>
{
    var role = roleService.GetRoleById(id);
    return role is null ? Results.NotFound() : Results.Ok(role);
})
.WithName("GetRoleById")
.WithOpenApi();

app.MapPut("/api/roles/{id:guid}/approve", async (Guid id, IRoleService roleService, IFeatureManager fm, ILogger<Program> logger) =>
{
    var requireApproval = await fm.IsEnabledAsync("RequireRoleApproval");
    if (!requireApproval)
    {
        return Results.BadRequest(new { error = "Role approval feature is not enabled" });
    }

    var role = roleService.GetRoleById(id);
    if (role is null)
    {
        return Results.NotFound();
    }

    roleService.ApproveRole(id);
    logger.LogInformation("Role approved: {RoleId} - {Title}", id, role.Title);
    
    return Results.Ok(new { message = "Role approved successfully", roleId = id });
})
.WithName("ApproveRole")
.WithOpenApi();

app.Run();

public partial class Program { }