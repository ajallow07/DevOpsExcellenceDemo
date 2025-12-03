# DevOps Excellence Demo

A .NET microservice demonstrating modern DevOps practices including TDD, feature toggles, CI/CD, security scanning, and observability.

## Application Description

This is a hiring status microservice built with ASP.NET Core that returns hiring decision messages based on a configurable feature flag. The application demonstrates enterprise-grade DevOps practices applied to a simple REST API.

**Key Features:**
- REST API endpoints for hiring status and job role management
- Job role posting with automatic 3-month expiration
- Feature toggles using Microsoft.FeatureManagement
- Test-Driven Development with unit and integration tests
- CI/CD pipeline with automated testing, security scanning, and code quality analysis
- Observability with Application Insights and structured logging

## Quick Start

```powershell
# Run the application
cd HiringApi
dotnet run

# Test the API (the app runs on http://localhost:5234)
curl http://localhost:5234/api/hiring-status
curl http://localhost:5234/healthz

# Create a new job role (expires in 3 months)
Invoke-RestMethod -Method POST -Uri "http://localhost:5234/api/roles" `
  -ContentType "application/json" `
  -Body '{"title":"Senior Software Engineer","description":"Build scalable systems","department":"Engineering","location":"Remote"}'

# Get all active roles
curl http://localhost:5234/api/roles

# Get all roles including expired
curl http://localhost:5234/api/roles?includeExpired=true

# Run tests
cd ..
dotnet test HiringApi.Tests
```

## API Endpoints

### Hiring Status
- `GET /api/hiring-status` - Check hiring status (controlled by feature flag)

### Job Roles
- `POST /api/roles` - Create a new job role (automatically expires in 3 months)
- `GET /api/roles` - Get all active roles
- `GET /api/roles?includeExpired=true` - Get all roles including expired ones
- `GET /api/roles/{id}` - Get a specific role by ID

### Health
- `GET /healthz` - Health check endpoint

## Configuration

Toggle the hiring feature in `appsettings.json`:
```json
{
  "Features": {
    "Hired": false
  }
}
```

## How GitHub Copilot Helped

GitHub Copilot significantly accelerated this project's development:

**Code Generation**
- Generated REST API endpoints with proper async patterns
- Created comprehensive xUnit test cases (unit + integration)
- Suggested WebApplicationFactory for integration testing

**DevOps Configuration**
- Built complete GitHub Actions CI/CD pipeline
- Recommended security tools (Snyk, SonarCloud, Trivy)
- Created optimized multi-stage Dockerfile

**Problem Solving**
- Identified configuration override issue (appsettings.Development.json)
- Suggested Microsoft.FeatureManagement for feature toggles
- Provided Application Insights integration patterns

**Impact:** ~70% time reduction on boilerplate, configuration, and DevOps setup.

## Tech Stack

.NET 10.0 • ASP.NET Core • Microsoft.FeatureManagement • Application Insights • xUnit • GitHub Actions • SonarCloud • Snyk • Docker
