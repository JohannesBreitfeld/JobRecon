# CLAUDE.md - JobRecon Project Guidelines

## Project Overview

JobRecon is a microservices-based job matching platform that helps users find relevant job postings automatically using AI-based semantic matching. This is a portfolio project demonstrating senior backend and cloud architecture skills.

**Architecture:** See `docs/architecture/ARCHITECTURE.md` for full details.

## Tech Stack

| Layer | Technology |
|-------|------------|
| Runtime | .NET 10, C# |
| Web Framework | ASP.NET Core Minimal APIs |
| Frontend | React 18 + TypeScript + MUI |
| API Docs | NSwag (OpenAPI + TypeScript client gen) |
| Identity | ASP.NET Core Identity + JWT Bearer |
| Database | PostgreSQL (relational), MongoDB (jobs) |
| Vector DB | Qdrant |
| Messaging | MassTransit + RabbitMQ |
| AI/LLM | Ollama (Mistral 7B, nomic-embed-text) |
| Container | Docker, k3s (Kubernetes) |
| CI/CD | GitHub Actions |
| GitOps | ArgoCD |

## Critical Rules

### NEVER Commit Secrets

> **THIS IS NON-NEGOTIABLE. ZERO EXCEPTIONS.**

- NEVER put passwords, API keys, connection strings, or tokens in code
- NEVER put secrets in appsettings.json or any committed file
- ALWAYS use:
  - `dotnet user-secrets` for local development
  - Kubernetes Secrets / Sealed Secrets for k3s
  - GitHub Secrets for CI/CD
  - Azure Key Vault for production
- If you see a secret in code, STOP and ask how to fix it

```csharp
// WRONG - Never do this
"ConnectionStrings": {
  "Default": "Host=localhost;Password=mysecret123"  // NEVER!
}

// CORRECT - Empty placeholder, populated from secrets
"ConnectionStrings": {
  "Default": ""  // Loaded from user-secrets or environment
}
```

### Always Ask First

Before making significant changes, ASK the user:
- Adding new NuGet packages or npm dependencies
- Changing architectural patterns
- Modifying database schema
- Creating new services or projects
- Changing authentication/authorization logic
- Any security-related changes

### File Issues for Out-of-Scope Discoveries

When you discover bugs, missing features, or technical debt that is outside the scope of the current task, create a GitHub issue for it instead of fixing it inline. Use `gh issue create` with an appropriate title prefix (`bug:`, `feat:`, `chore:`) and include steps to reproduce, expected behavior, and root cause if known.

## C# Coding Conventions

### Naming
- `PascalCase` for public members, types, methods, properties
- `_camelCase` for private fields (with underscore prefix)
- `camelCase` for local variables and parameters
- `IPascalCase` for interfaces
- `TPascalCase` for generic type parameters

### Style
- File-scoped namespaces (`namespace JobRecon.Services;`)
- Primary constructors for simple classes
- Expression-bodied members for simple getters
- `var` when type is obvious, explicit type when it adds clarity
- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- Treat nullable warnings as errors

### Documentation
- XML docs on public APIs only (`/// <summary>`)
- No comments for self-explanatory code
- Comments only for complex business logic or non-obvious decisions
- Keep code readable enough to not need comments

### Example

```csharp
namespace JobRecon.Services.Profile;

public sealed class ProfileService(
    IProfileRepository repository,
    ILogger<ProfileService> logger) : IProfileService
{
    public async Task<Result<ProfileDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var profile = await repository.FindByIdAsync(id, ct);

        if (profile is null)
            return Result.Failure<ProfileDto>(ProfileErrors.NotFound(id));

        return Result.Success(profile.ToDto());
    }
}
```

## Error Handling

Use the **Result pattern** - avoid exceptions for control flow:

```csharp
// Define errors as static classes
public static class ProfileErrors
{
    public static Error NotFound(Guid id) => new("Profile.NotFound", $"Profile {id} not found");
    public static Error InvalidEmail => new("Profile.InvalidEmail", "Email format is invalid");
}

// Return Result<T> instead of throwing
public async Task<Result<Profile>> CreateAsync(CreateProfileRequest request)
{
    if (!IsValidEmail(request.Email))
        return Result.Failure<Profile>(ProfileErrors.InvalidEmail);

    var profile = new Profile(request);
    await _repository.AddAsync(profile);

    return Result.Success(profile);
}
```

Exceptions are only for:
- Truly unexpected situations (bugs, infrastructure failures)
- Third-party library boundaries
- Startup/configuration errors

## Project Structure

```
JobRecon/
├── src/
│   ├── Shared/
│   │   ├── JobRecon.Contracts/      # DTOs, events, shared models
│   │   ├── JobRecon.Domain/         # Domain entities, value objects
│   │   └── JobRecon.Infrastructure/ # Cross-cutting concerns
│   │
│   ├── Services/
│   │   ├── JobRecon.Identity/       # Auth service (port 5001)
│   │   ├── JobRecon.Profile/        # Profile service (port 5002)
│   │   ├── JobRecon.Jobs/           # Jobs service (port 5003)
│   │   ├── JobRecon.Matching/       # Matching service (port 5005)
│   │   └── JobRecon.Notifications/  # Notifications service (port 5006)
│   │
│   └── Gateway/
│       └── JobRecon.Gateway/        # API Gateway
│
├── frontend/                        # React + TypeScript app
├── tests/                           # Test projects
├── deploy/                          # Kubernetes, Helm, Docker
└── docs/                            # Architecture documentation
```

## TypeScript/React Conventions

### Naming
- `PascalCase` for components, types, interfaces
- `camelCase` for functions, variables, hooks
- `SCREAMING_SNAKE_CASE` for constants
- Prefix hooks with `use` (e.g., `useProfile`)

### Style
- Functional components only (no class components)
- Use TypeScript strict mode
- Prefer `interface` over `type` for object shapes
- Use MUI components and theming
- TanStack Query for server state, Zustand for client state

```typescript
// Example component
interface ProfileFormProps {
  initialData?: ProfileDto;
  onSubmit: (data: ProfileUpdateDto) => void;
}

export function ProfileForm({ initialData, onSubmit }: ProfileFormProps) {
  const { register, handleSubmit } = useForm<ProfileUpdateDto>({
    defaultValues: initialData,
  });

  return (
    <Box component="form" onSubmit={handleSubmit(onSubmit)}>
      <TextField {...register('firstName')} label="First Name" />
      <Button type="submit" variant="contained">Save</Button>
    </Box>
  );
}
```

## Git Conventions

### Commit Messages

Use **Conventional Commits**:

```
feat: add profile skills management
fix: resolve JWT token refresh race condition
chore: update NuGet packages
docs: add API documentation for profile endpoints
refactor: extract job matching logic to dedicated service
test: add unit tests for ProfileService
```

### Branch Names

```
feature/add-profile-skills
fix/jwt-token-refresh
chore/update-dependencies
```

### Pull Requests

- Keep PRs focused and small
- Reference related issues
- Ensure CI passes before requesting review

### Close Issues on Push

When working on a GitHub issue, always close it when pushing the fix. Use `Closes #<number>` or `Fixes #<number>` in the commit message or PR body so the issue is closed automatically when the branch is merged.

## Testing Guidelines

- Write tests **when explicitly requested**
- Use xUnit for .NET tests
- Use Vitest + Testing Library for React tests
- Integration tests should use Testcontainers
- Name tests: `MethodName_Scenario_ExpectedResult`

```csharp
[Fact]
public async Task GetByIdAsync_WhenProfileExists_ReturnsProfile()
{
    // Arrange
    var profileId = Guid.NewGuid();
    _repository.FindByIdAsync(profileId, Arg.Any<CancellationToken>())
        .Returns(new Profile { Id = profileId });

    // Act
    var result = await _sut.GetByIdAsync(profileId);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Id.Should().Be(profileId);
}
```

## Common Commands

```bash
# Build solution
dotnet build

# Run tests
dotnet test

# Run specific service
dotnet run --project src/Services/JobRecon.Identity

# Generate TypeScript clients
nswag run nswag.json

# Frontend dev server
cd frontend && npm run dev

# Docker compose (local dev)
docker compose -f deploy/docker/docker-compose.yml up -d
```

## What Claude Should Do

- Follow the architecture patterns in `docs/architecture/ARCHITECTURE.md`
- Use existing abstractions and patterns in the codebase
- Keep code simple and readable
- Validate inputs at API boundaries
- Use async/await consistently
- Prefer composition over inheritance
- Use dependency injection

## What Claude Should NOT Do

- Add secrets to any file
- Create large monolithic files (split appropriately)
- Add dependencies without asking
- Change architectural patterns without asking
- Ignore nullable warnings
- Use `dynamic` or `object` when a proper type exists
- Skip validation for user input
- Use synchronous I/O operations
- Create circular dependencies between projects
