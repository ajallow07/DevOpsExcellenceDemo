using Microsoft.FeatureManagement;
using HiringApi.Models;
using HiringApi.Services;
using HiringApi.Configuration;
using HiringApi.Validation;
using HiringApi.Middleware;
using System.Threading.RateLimiting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Application Insights and Logging
builder.Services.AddApplicationInsightsTelemetry();
builder.Logging.AddApplicationInsights();

// Feature Management
builder.Services.AddFeatureManagement(builder.Configuration.GetSection("Features"));

// Health Checks
builder.Services.AddHealthChecks();

// Configuration
builder.Services.Configure<RoleSettings>(builder.Configuration.GetSection("RoleSettings"));

// Services
builder.Services.AddSingleton<IRoleService, RoleService>();
builder.Services.AddSingleton<IRoleValidator, RoleValidator>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" })
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("X-Api-Version");
    });
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { error = "Rate limit exceeded. Please try again later." },
            cancellationToken: cancellationToken);
    };
});

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hiring API",
        Version = "v1",
        Description = "API for managing job roles with feature flags and approval workflows",
        Contact = new OpenApiContact
        {
            Name = "DevOps Excellence Team",
            Url = new Uri("https://github.com/ajallow07/DevOpsExcellenceDemo")
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Middleware pipeline
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Hiring API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseRateLimiter();
app.UseCors("DefaultPolicy");

app.MapHealthChecks("/healthz");

app.MapGet("/api/v1/hiring-status", async (IFeatureManager fm, ILogger<Program> logger) =>
{
    var hired = await fm.IsEnabledAsync("Hired");
    var msg = hired 
        ? "Congratulations! We're thrilled to offer you a position on our team. Welcome aboard!" 
        : "Thank you for your interest in joining our team. While we were impressed with your qualifications, we've decided to move forward with other candidates at this time. We wish you the best in your job search!";
    logger.LogInformation("Hiring status checked. Hired={Hired}", hired);
    return Results.Ok(new { hired, message = msg });
})
.WithName("HiringStatus")
.WithOpenApi(operation =>
{
    operation.Summary = "Check hiring status";
    operation.Description = "Returns current hiring status based on the Hired feature flag.";
    return operation;
});

app.MapPost("/api/v1/roles", async (CreateRoleRequest request, IRoleService roleService, IRoleValidator validator, IFeatureManager fm, ILogger<Program> logger) =>
{
    // Check if role posting is enabled
    var rolePostingEnabled = await fm.IsEnabledAsync("EnableRolePosting");
    if (!rolePostingEnabled)
    {
        logger.LogWarning("Role posting attempt blocked - feature disabled");
        return Results.Problem(
            detail: "Role posting is currently disabled",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    // Validate input
    var (isValid, errorMessage) = validator.ValidateCreateRequest(request);
    if (!isValid)
    {
        logger.LogWarning("Invalid role creation request: {Error}", errorMessage);
        return Results.BadRequest(new { error = errorMessage });
    }
    
    var requireApproval = await fm.IsEnabledAsync("RequireRoleApproval");
    var role = roleService.CreateRole(request, requireApproval);
    
    logger.LogInformation("New role posted: {RoleId} - {Title}, RequiresApproval: {RequireApproval}", 
        role.Id, role.Title, requireApproval);
    
    return Results.Created($"/api/v1/roles/{role.Id}", role);
})
.WithName("CreateRole")
.WithOpenApi(operation =>
{
    operation.Summary = "Create a new job role";
    operation.Description = "Creates a new job role with automatic expiration. Requires EnableRolePosting feature flag.";
    return operation;
});

app.MapGet("/api/v1/roles", async (IRoleService roleService, IFeatureManager fm, bool includeExpired = false) =>
{
    var showExpired = await fm.IsEnabledAsync("ShowExpiredRoles");
    var roles = roleService.GetAllRoles(includeExpired || showExpired, includeUnapproved: false);
    return Results.Ok(roles);
})
.WithName("GetRoles")
.WithOpenApi(operation =>
{
    operation.Summary = "Get all job roles";
    operation.Description = "Retrieves all active job roles. Expired roles are excluded unless ShowExpiredRoles feature flag is enabled.";
    return operation;
});

app.MapGet("/api/v1/roles/{id:guid}", (Guid id, IRoleService roleService, ILogger<Program> logger) =>
{
    var role = roleService.GetRoleById(id);
    if (role is null)
    {
        logger.LogWarning("Role not found: {RoleId}", id);
        return Results.NotFound(new { error = "Role not found", roleId = id });
    }
    return Results.Ok(role);
})
.WithName("GetRoleById")
.WithOpenApi(operation =>
{
    operation.Summary = "Get a specific job role";
    operation.Description = "Retrieves a job role by its unique identifier.";
    return operation;
});

app.MapPut("/api/v1/roles/{id:guid}/approve", async (Guid id, IRoleService roleService, IFeatureManager fm, ILogger<Program> logger) =>
{
    var requireApproval = await fm.IsEnabledAsync("RequireRoleApproval");
    if (!requireApproval)
    {
        logger.LogWarning("Role approval attempt when feature is disabled: {RoleId}", id);
        return Results.BadRequest(new { error = "Role approval feature is not enabled" });
    }

    var role = roleService.GetRoleById(id);
    if (role is null)
    {
        logger.LogWarning("Approval attempted for non-existent role: {RoleId}", id);
        return Results.NotFound(new { error = "Role not found", roleId = id });
    }

    if (role.IsApproved)
    {
        logger.LogInformation("Role already approved: {RoleId}", id);
        return Results.Ok(new { message = "Role is already approved", roleId = id });
    }

    roleService.ApproveRole(id);
    logger.LogInformation("Role approved: {RoleId} - {Title}", id, role.Title);
    
    return Results.Ok(new { message = "Role approved successfully", roleId = id });
})
.WithName("ApproveRole")
.WithOpenApi(operation =>
{
    operation.Summary = "Approve a job role";
    operation.Description = "Approves a job role pending approval. Requires RequireRoleApproval feature flag.";
    return operation;
});

app.Run();

public partial class Program { }