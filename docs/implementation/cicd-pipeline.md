# 🚀 CI/CD Pipeline

---

# 1. PURPOSE

Định nghĩa pipeline cho build, test, và deploy hệ thống plugin runtime.

---

# 2. PIPELINE OVERVIEW

```
Push to branch
      ↓
Build + Restore
      ↓
Unit Tests
      ↓
Integration Tests (Docker)
      ↓
Security Scan (dependencies)
      ↓
Code Quality Check
      ↓
Docker Image Build
      ↓
Push to Registry
      ↓
Deploy to Staging
      ↓
Smoke Tests
      ↓
Manual Approval
      ↓
Deploy to Production
```

---

# 3. CI STAGES

## Stage 1: Build

```yaml
- dotnet restore
- dotnet build --configuration Release --no-restore
```

## Stage 2: Unit Tests

```yaml
- dotnet test --filter "Category=Unit" --configuration Release --no-build
```

## Stage 3: Integration Tests

Requires Docker (Testcontainers).

```yaml
- dotnet test --filter "Category=Integration" --configuration Release --no-build
```

## Stage 4: Security Scan

```yaml
- dotnet list package --vulnerable --include-transitive
```

Optional: Trivy/Snyk scan on Docker image.

## Stage 5: Publish

```yaml
- dotnet publish src/API/PluginRuntime.Api -c Release -o ./publish
```

---

# 4. DOCKER BUILD

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish src/API/PluginRuntime.Api -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "PluginRuntime.Api.dll"]
```

---

# 5. BRANCH STRATEGY

| Branch | Purpose | Deploy to |
|--------|---------|-----------|
| main | Production-ready | Production (after approval) |
| develop | Integration | Staging |
| feature/* | Feature work | — |
| hotfix/* | Emergency fix | Production (fast track) |

---

# 6. QUALITY GATES

Build fails if:

- Any unit test fails
- Any integration test fails
- Vulnerable dependency detected (critical/high)
- Code coverage drops below threshold (70%)

---

# 7. DEPLOYMENT MODEL

- Blue/Green deployment (preferred)
- Rolling update (alternative)
- Zero-downtime requirement

---

# 8. ENVIRONMENT PIPELINE

```
feature branch → PR → develop → staging → main → production
```

---

# 9. ROLLBACK

- Keep previous Docker image tagged
- Database migrations must be backward-compatible
- Rollback = redeploy previous image tag

---

# 🏁 END
