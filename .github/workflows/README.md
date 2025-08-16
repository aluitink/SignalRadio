# GitHub Workflows Documentation

This repository includes several GitHub Actions workflows for continuous integration and quality assurance.

## Workflows

### 1. CI (`ci.yml`)
**Triggers:** Push to `main`/`develop`, PRs to `main`

**Purpose:** Core continuous integration workflow that builds, tests, and publishes the application.

**Steps:**
- Setup .NET 9.0
- Restore NuGet packages
- Build the solution in Release configuration
- Run unit tests
- Publish the API project
- Upload build artifacts

### 2. Docker Build (`docker-build.yml.disabled`)
**Status:** DISABLED - No image repository configured

**Purpose:** Builds Docker images without pushing to registry (for CI validation).

**To Enable:** Rename `docker-build.yml.disabled` to `docker-build.yml` when ready to use Docker builds.

**Steps:**
- Setup Docker Buildx
- Build Docker image using `./docker/Dockerfile.api`
- Test the built image by running it and hitting the health endpoint
- Uses GitHub Actions cache for faster builds

### 3. Code Quality (`code-quality.yml`)
**Triggers:** Push to `main`/`develop`, PRs to `main`

**Purpose:** Ensures code quality and formatting standards.

**Steps:**
- Check code formatting with `dotnet format`
- Run static analysis
- Security scan for vulnerable packages

### 4. Dependency Check (`dependency-check.yml`)
**Triggers:** Weekly schedule (Mondays 9 AM UTC), manual dispatch

**Purpose:** Regular monitoring of package dependencies.

**Steps:**
- Check for outdated packages
- Scan for vulnerable packages
- Generate reports as artifacts

## Artifacts

The workflows generate the following artifacts:

- **published-app**: Complete published application (30-day retention)
- **dependency-reports**: Package analysis reports (30-day retention)

## Setup Requirements

### For Docker workflows to work properly:
1. Ensure the API has a health endpoint (already added at `/health`)
2. Docker build context includes all necessary files

### For future enhancements:
1. **Image Registry**: When ready to push images, update `docker-build.yml` with:
   - Docker registry login
   - Set `push: true`
   - Configure proper image tags

2. **Test Projects**: Add test projects to enable proper test execution

3. **Code Coverage**: Add code coverage reporting tools

## Usage

All workflows run automatically on code changes. The dependency check can also be triggered manually from the Actions tab in GitHub.

## Security

- No secrets are currently required
- When adding image registry, use GitHub secrets for credentials
- Vulnerability scanning runs automatically on all dependencies
