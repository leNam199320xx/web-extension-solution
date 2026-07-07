# Tech Stack & Build

## Runtime

- .NET 10
- C#
- ASP.NET Core (API Gateway + Plugin Controller API)

## Key Technologies

- **Plugin Isolation**: AssemblyLoadContext (ALC) for dynamic loading/unloading
- **Security**: RSA/ECDSA digital signatures, SHA-256 hash verification, HSM/KMS for key management
- **Infrastructure**: PostgreSQL (persistence), Redis (cache), external object storage
- **Observability**: OpenTelemetry, structured JSON logging (ELK-compatible)
- **Dependency Injection**: built-in .NET DI (Core only, not in plugins)

## Architecture Patterns

- Zero-Trust security model
- Capability-Based Access Control
- Stateless Core Runtime
- Fail-closed execution
- Plugin sandboxing via ALC (isolation, not security boundary)
- CancellationToken propagation on all async paths

## Coding Standards

### Must

- async/await for all I/O
- CancellationToken on every async method
- Validate input at boundaries
- Small, focused services
- Strongly typed contracts
- Explicit code over clever abstractions

### Must Not

- Blocking async calls (`.Result`, `.Wait()`)
- Global static state
- Reflection for security-sensitive logic
- Direct DB/infra access from plugins
- Deep inheritance hierarchies
- Over-engineering (no factory-for-the-sake-of-factory)

## Commands

> **Note**: No implementation exists yet (0% complete). Commands below are anticipated based on .NET conventions. Update this section once the solution file is created.

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests
dotnet test

# Run the API
dotnet run --project src/<ProjectName>
```

## Decision Hierarchy

When in doubt: **Security > Performance > Convenience**

Choose the simplest secure implementation.
