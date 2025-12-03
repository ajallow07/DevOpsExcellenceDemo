using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace tests;

public class HiringProcessTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HiringProcessTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HiringStatus_WhenHiredFeatureEnabled_ReturnsWelcomeMessage()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Features:Hired"] = "true"
                });
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/hiring-status");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<HiringStatusResponse>();
        Assert.NotNull(result);
        Assert.True(result.hired);
        Assert.Contains("Congratulations", result.message);
        Assert.Contains("Welcome aboard", result.message);
    }

    [Fact]
    public async Task HiringStatus_WhenHiredFeatureDisabled_ReturnsRejectionMessage()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Features:Hired"] = "false"
                });
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/hiring-status");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<HiringStatusResponse>();
        Assert.NotNull(result);
        Assert.False(result.hired);
        Assert.Contains("Thank you for your interest", result.message);
        Assert.Contains("other candidates", result.message);
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/healthz");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HiringStatus_ReturnsJsonResponse()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/hiring-status");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    private record HiringStatusResponse(bool hired, string message);
}
