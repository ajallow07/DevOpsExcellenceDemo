using Microsoft.FeatureManagement;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry();
builder.Logging.AddApplicationInsights();

builder.Services.AddFeatureManagement(builder.Configuration.GetSection("Features"));
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/healthz");

app.MapGet("/api/hiring-status", async (IFeatureManager fm, ILogger<Program> logger) =>
{
    var hired = await fm.IsEnabledAsync("Hired");
    var msg = hired 
        ? "🎉 Congratulations! We're thrilled to offer you a position on our team. Welcome aboard!" 
        : "Thank you for your interest in joining our team. While we were impressed with your qualifications, we've decided to move forward with other candidates at this time. We wish you the best in your job search!";
    logger.LogInformation("Hiring status checked. Hired={Hired}", hired);
    return Results.Ok(new { hired, message = msg });
})
.WithName("HiringStatus")
.WithOpenApi();

app.Run();

public partial class Program { }