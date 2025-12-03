using Microsoft.FeatureManagement;
using HiringApi.Models;
using HiringApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry();
builder.Logging.AddApplicationInsights();

builder.Services.AddFeatureManagement(builder.Configuration.GetSection("Features"));
builder.Services.AddHealthChecks();
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

app.MapPost("/api/roles", (CreateRoleRequest request, IRoleService roleService, ILogger<Program> logger) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
        return Results.BadRequest(new { error = "Title is required" });
    
    if (string.IsNullOrWhiteSpace(request.Description))
        return Results.BadRequest(new { error = "Description is required" });
    
    var role = roleService.CreateRole(request);
    logger.LogInformation("New role posted: {RoleId} - {Title}", role.Id, role.Title);
    
    return Results.Created($"/api/roles/{role.Id}", role);
})
.WithName("CreateRole")
.WithOpenApi();

app.MapGet("/api/roles", (IRoleService roleService, bool includeExpired = false) =>
{
    var roles = roleService.GetAllRoles(includeExpired);
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

app.Run();

public partial class Program { }