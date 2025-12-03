# DevOps Excellence Demo

A .NET microservice demonstrating modern DevOps practices including TDD, feature toggles, CI/CD, security scanning, and observability.

## Application Description

This is a hiring status microservice built with ASP.NET Core that returns hiring decision messages based on a configurable feature flag. The application demonstrates enterprise-grade DevOps practices applied to a simple REST API.

**Key Features:**
- REST API endpoints for hiring status and job role management
- Job role posting with configurable expiration period
- Advanced feature flags for role management (posting control, approval workflow, visibility)
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
- `POST /api/roles` - Create a new job role (configurable expiration, optional approval workflow)
- `GET /api/roles` - Get all active, approved roles
- `GET /api/roles?includeExpired=true` - Get all roles including expired ones
- `GET /api/roles/{id}` - Get a specific role by ID
- `PUT /api/roles/{id}/approve` - Approve a role (when approval workflow is enabled)

### Health
- `GET /healthz` - Health check endpoint

## Configuration

### Feature Flags
Control application behavior through `appsettings.json`:

```json
{
  "Features": {
    "Hired": false,
    "EnableRolePosting": true,      // Kill switch for role creation
    "RequireRoleApproval": false,   // Enable approval workflow
    "ShowExpiredRoles": false       // Control expired role visibility
  },
  "RoleSettings": {
    "ExpirationMonths": 3           // Configurable expiration period
  }
}
```

**Feature Flag Capabilities:**
- **EnableRolePosting**: Emergency kill switch to disable role creation (returns 503)
- **RequireRoleApproval**: Toggle approval workflow - roles created as unapproved when enabled
- **ShowExpiredRoles**: Control whether expired roles appear in API responses
- **ExpirationMonths**: Configure how long roles remain active (1-12 months)

## Feature Flag Use Cases

### Production Scenarios

**1. Gradual Rollout**
```json
{
  "Features": {
    "EnableRolePosting": true,
    "RequireRoleApproval": true  // Start with approval for safety
  }
}
```
Enable role posting with approval workflow, then gradually remove approval requirement after validation.

**2. Emergency Response**
```json
{
  "Features": {
    "EnableRolePosting": false  // Kill switch activated
  }
}
```
Instantly disable role creation during incidents without code deployment.

**3. A/B Testing**
```json
{
  "RoleSettings": {
    "ExpirationMonths": 6  // Test longer expiration periods
  }
}
```
Test different expiration periods to optimize application rates.

**4. Environment-Specific Behavior**
- **Production**: Hide expired roles, require approval
- **Staging**: Show expired roles, no approval (faster testing)
- **Development**: All features enabled for full testing

## How GitHub Copilot Helped

GitHub Copilot significantly accelerated this project's development:

**Code Generation**
- Generated REST API endpoints with proper async patterns
- Created comprehensive xUnit test cases (unit + integration)
- Suggested WebApplicationFactory for integration testing
- Implemented feature flag integration patterns

**DevOps Configuration**
- Built complete GitHub Actions CI/CD pipeline
- Recommended security tools (Snyk, SonarCloud, Trivy)
- Created optimized multi-stage Dockerfile

**Problem Solving**
- Identified configuration override issue (appsettings.Development.json)
- Suggested Microsoft.FeatureManagement for feature toggles
- Provided Application Insights integration patterns
- Designed approval workflow with feature flags

**Impact:** ~70% time reduction on boilerplate, configuration, and DevOps setup.

## CI/CD Pipelines

### Build and Test Pipeline
Automated CI/CD workflow that runs on every push and pull request:

**Pipeline: `.github/workflows/ci-cd.yml`**
- ✅ Restore dependencies
- ✅ Build in Release mode
- ✅ Run all unit and integration tests
- ✅ Collect code coverage
- 🔧 SonarCloud code quality scan (optional)

**Triggers:**
- Push to `main` or `develop` branches
- Pull requests to `main`

### Docker Image Pipeline
Automated Docker build and publish workflow:

**Pipeline: `.github/workflows/docker-publish.yml`**
- 🐳 Build Docker image using multi-stage Dockerfile
- 🚀 Push to Docker Hub registry
- 🏷️ Tag with multiple versions:
  - `latest` - Latest stable build
  - `build-<number>` - Build number (e.g., `build-42`)
  - `build-<sha>` - Git commit SHA (e.g., `build-a1b2c3d`)

**Triggers:**
- PR merge to `main` branch
- Manual workflow dispatch

**Docker Hub Setup:**
1. Add repository secrets in GitHub:
   - `DOCKER_USERNAME`: Your Docker Hub username
   - `DOCKER_PASSWORD`: Docker Hub access token
2. Image will be published as: `<username>/hiringapi:latest`

### Code Quality with SonarCloud

**Setup SonarCloud Integration:**
1. Sign up at https://sonarcloud.io with GitHub
2. Import your repository
3. Generate token at https://sonarcloud.io/account/security
4. Add `SONAR_TOKEN` to GitHub repository secrets
5. Uncomment SonarCloud step in `ci-cd.yml`

**Configuration:** `sonar-project.properties`
- Code coverage analysis
- Security vulnerability detection
- Code smell identification
- Duplication detection

## Tech Stack

.NET 10.0 • ASP.NET Core • Microsoft.FeatureManagement • Application Insights • xUnit • GitHub Actions • Docker Hub • SonarCloud • Snyk
