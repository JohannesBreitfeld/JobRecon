#!/bin/bash
# Create GitHub issues for MVP plan
# Prerequisites: gh auth login
# Usage: bash scripts/create-mvp-issues.sh

set -e

REPO=$(gh repo view --json nameWithOwner -q .nameWithOwner)
echo "Creating issues in $REPO..."

# --- Issue 1: Proto Contracts ---
gh issue create \
  --title "feat: Define gRPC proto contracts for inter-service communication" \
  --label "enhancement,mvp" \
  --body "$(cat <<'EOF'
## Summary
Create shared `.proto` files to define gRPC service contracts for internal service-to-service communication. This replaces the current HTTP client calls between services with type-safe, high-performance gRPC.

## Scope

### New project: `src/Shared/JobRecon.Protos/`

**`profile_service.proto`:**
- `GetProfile(GetProfileRequest) → ProfileResponse` — returns full profile with skills, desired titles, and job preferences for a given userId
- `GetUserEmail(GetUserEmailRequest) → UserEmailResponse` — returns email + display name for notifications

**`jobs_service.proto`:**
- `GetActiveJobs(GetActiveJobsRequest) → JobListResponse` — paginated list of active jobs with all fields needed by matching
- `GetJob(GetJobRequest) → JobResponse` — single job by ID with full details

### `JobRecon.Protos.csproj`
- net10.0 class library
- References: `Grpc.Tools`, `Google.Protobuf`
- Generates both server and client code
- Add to `JobRecon.slnx`

## Acceptance Criteria
- [ ] Proto files compile without errors
- [ ] Generated C# code is accessible from other projects via project reference
- [ ] Message types cover all fields currently exchanged via HTTP (see matching ProfileDto, JobDto, JobListDto)
- [ ] Solution builds successfully

## Context
Currently Matching calls Profile and Jobs via HTTP REST. Notifications calls a Profile endpoint that doesn't even exist. gRPC gives us compile-time type safety and better performance for internal calls.
EOF
)"

echo "Created issue: Proto Contracts"

# --- Issue 2: gRPC Server — Profile ---
gh issue create \
  --title "feat: Add gRPC server to Profile service" \
  --label "enhancement,mvp" \
  --body "$(cat <<'EOF'
## Summary
Implement the `ProfileGrpc` gRPC service in the Profile service to serve internal requests from Matching and Notifications services.

## Scope

### New files
- `src/Services/JobRecon.Profile/Grpc/ProfileGrpcService.cs` — implements `ProfileGrpc.ProfileGrpcBase`
  - `GetProfile`: query ProfileDbContext by userId, return full profile with skills, desired titles, preferences
  - `GetUserEmail`: query for user email and display name (currently missing as an HTTP endpoint)

### Modified files
- `JobRecon.Profile.csproj` — add `Grpc.AspNetCore` NuGet, reference `JobRecon.Protos`
- `Program.cs` — register gRPC services, map gRPC endpoint
- `appsettings.json` — configure Kestrel for dual-port: HTTP on 5002 (REST for gateway), HTTP/2 on 5012 (gRPC for internal)

## Acceptance Criteria
- [ ] Profile service starts with both HTTP and gRPC ports
- [ ] `GetProfile` returns full profile data when called via gRPC
- [ ] `GetUserEmail` returns email + display name
- [ ] Existing REST endpoints continue to work unchanged
- [ ] No JWT required for gRPC calls (internal service-to-service)

## Notes
- gRPC service reads directly from ProfileDbContext, bypassing the REST endpoint layer
- This is internal-only communication, no auth needed (services trust each other within the cluster)
EOF
)"

echo "Created issue: gRPC Server — Profile"

# --- Issue 3: gRPC Server — Jobs ---
gh issue create \
  --title "feat: Add gRPC server to Jobs service" \
  --label "enhancement,mvp" \
  --body "$(cat <<'EOF'
## Summary
Implement the `JobsGrpc` gRPC service in the Jobs service to serve internal requests from the Matching service.

## Scope

### New files
- `src/Services/JobRecon.Jobs/Grpc/JobsGrpcService.cs` — implements `JobsGrpc.JobsGrpcBase`
  - `GetActiveJobs`: query active jobs with pagination (limit, offset), return full job data
  - `GetJob`: query single job by ID with company info, skills, tags

### Modified files
- `JobRecon.Jobs.csproj` — add `Grpc.AspNetCore`, reference `JobRecon.Protos`
- `Program.cs` — register gRPC services, map gRPC endpoint
- `appsettings.json` — configure Kestrel dual-port: HTTP on 5003, HTTP/2 on 5013

## Acceptance Criteria
- [ ] Jobs service starts with both HTTP and gRPC ports
- [ ] `GetActiveJobs` returns paginated active jobs with all matching-relevant fields
- [ ] `GetJob` returns complete job details including company, skills, salary
- [ ] Existing REST endpoints continue to work unchanged
- [ ] `TotalCount` is included in response for pagination

## Notes
- Currently the Matching service fetches up to 5000 jobs in batches of 100 via HTTP. The gRPC version should support the same pagination pattern.
EOF
)"

echo "Created issue: gRPC Server — Jobs"

# --- Issue 4: gRPC Clients — Matching ---
gh issue create \
  --title "feat: Replace HTTP clients with gRPC in Matching service" \
  --label "enhancement,mvp" \
  --body "$(cat <<'EOF'
## Summary
Replace the HTTP-based `ProfileClient` and `JobsClient` in the Matching service with gRPC clients using the shared proto contracts.

## Scope

### Modified files
- `Services/ProfileClient.cs` — rewrite to use `ProfileGrpc.ProfileGrpcClient`
- `Services/JobsClient.cs` — rewrite to use `JobsGrpc.JobsGrpcClient`
- `JobRecon.Matching.csproj` — add `Grpc.Net.Client`, `Google.Protobuf`, reference `JobRecon.Protos`
- `Extensions/ServiceCollectionExtensions.cs` — replace `AddHttpClient<>` registrations with gRPC channel factory
- `appsettings.json` / `appsettings.Development.json` — replace `ServiceUrls` section with `GrpcServices` addresses

### Configuration
```json
"GrpcServices": {
  "ProfileService": "http://localhost:5012",
  "JobsService": "http://localhost:5013"
}
```

## Acceptance Criteria
- [ ] Matching service connects to Profile and Jobs via gRPC
- [ ] `GetRecommendationsAsync` works end-to-end with gRPC
- [ ] `GetJobMatchScoreAsync` works with gRPC
- [ ] Old HTTP client code and `ServiceUrls` config removed
- [ ] Retry policy maintained (or use gRPC built-in retry)

## Dependencies
- Proto contracts (issue #1)
- gRPC server in Profile (issue #2)
- gRPC server in Jobs (issue #3)
EOF
)"

echo "Created issue: gRPC Clients — Matching"

# --- Issue 5: gRPC Client — Notifications ---
gh issue create \
  --title "feat: Replace HTTP client with gRPC in Notifications service" \
  --label "enhancement,mvp" \
  --body "$(cat <<'EOF'
## Summary
Replace the HTTP-based `ProfileClient` in the Notifications service with a gRPC client. The current HTTP client calls `GET /api/profile/{userId}/email` which doesn't even exist as an endpoint.

## Scope

### Modified files
- `Contracts/ProfileClient.cs` — rewrite to use `ProfileGrpc.ProfileGrpcClient.GetUserEmail()`
- `JobRecon.Notifications.csproj` — add `Grpc.Net.Client`, reference `JobRecon.Protos`
- `Extensions/ServiceCollectionExtensions.cs` — replace `AddHttpClient<>` with gRPC channel
- `appsettings.json` — add gRPC address for Profile service

## Acceptance Criteria
- [ ] Notifications service connects to Profile via gRPC
- [ ] `GetUserEmailAsync` works and returns email + display name
- [ ] Email notifications can be sent with correct recipient
- [ ] Old HTTP client code removed

## Dependencies
- Proto contracts (issue #1)
- gRPC server in Profile (issue #2)
EOF
)"

echo "Created issue: gRPC Client — Notifications"

# --- Issue 6: Fix Job Fetch Trigger ---
gh issue create \
  --title "fix: Implement manual job fetch trigger in Jobs service" \
  --label "bug,mvp" \
  --body "$(cat <<'EOF'
## Summary
The `TriggerFetchAsync` method in `JobSourceService.cs` has commented-out code preventing manual job fetching via API. This needs to work so we can trigger job downloads on demand instead of waiting for the daily 6 AM cron schedule.

## Scope

### Modified files
- `src/Services/JobRecon.Jobs/Services/JobSourceService.cs` (~line 202)

## Acceptance Criteria
- [ ] Manual trigger via API endpoint works
- [ ] Triggering a fetch downloads jobs from JobTech Links
- [ ] Proper error handling if fetch is already in progress
- [ ] Returns meaningful response (job count fetched, errors)

## Context
For the MVP we need to be able to populate the job database immediately rather than waiting for the scheduler. The daily cron (`0 6 * * *`) is fine for steady state but not for initial setup/testing.
EOF
)"

echo "Created issue: Fix Job Fetch Trigger"

# --- Issue 7: AI/Vector Matching ---
gh issue create \
  --title "feat: Add Ollama + Qdrant vector matching to Matching service" \
  --label "enhancement,mvp" \
  --body "$(cat <<'EOF'
## Summary
Integrate Ollama (nomic-embed-text) for embedding generation and Qdrant for vector storage/search into the Matching service. This enables semantic job matching beyond the existing heuristic scoring.

## Scope

### 7a: Ollama Embedding Client
**New files:**
- `Clients/IOllamaClient.cs` — interface
- `Clients/OllamaClient.cs` — HTTP client calling Ollama `/api/embeddings`

Functionality:
- Generate embeddings using `nomic-embed-text` model
- For jobs: concatenate title + description + skills → embed
- For profiles: concatenate desired titles + skills + summary → embed

### 7b: Qdrant Vector Store
**New files:**
- `Clients/IVectorStore.cs` — interface
- `Clients/QdrantVectorStore.cs` — Qdrant client implementation

Functionality:
- Create `job_embeddings` collection on startup (if not exists)
- Upsert job vectors with metadata (jobId, title, company, location)
- Search by profile vector, return top-N similar job IDs with scores
- **NuGet:** `Qdrant.Client`

### 7c: Job Embedding Background Worker
**New file:**
- `Workers/JobEmbeddingWorker.cs` — `BackgroundService`

Functionality:
- Periodically check for un-embedded jobs (e.g., every 5 minutes)
- Fetch new/updated jobs via gRPC from Jobs service
- Generate embeddings via Ollama
- Store vectors in Qdrant
- Track embedded jobs to avoid re-processing

### 7d: Update Matching Pipeline
**Modified files:**
- `Services/MatchingService.cs` — hybrid scoring pipeline
- `Program.cs` — register new services
- `appsettings.json` — Ollama + Qdrant configuration

New matching flow:
1. Generate profile embedding from user skills/titles/preferences
2. Query Qdrant for top-100 semantically similar jobs
3. Fetch full job details via gRPC
4. Apply existing heuristic scoring on the shortlist
5. Combined score: 50% vector similarity + 50% heuristic
6. Fallback to heuristic-only if Qdrant is empty or Ollama unavailable

### Configuration
```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "EmbeddingModel": "nomic-embed-text"
  },
  "Qdrant": {
    "Host": "localhost",
    "Port": 6334,
    "CollectionName": "job_embeddings"
  }
}
```

## Acceptance Criteria
- [ ] Ollama client generates embeddings successfully
- [ ] Qdrant collection created and populated with job vectors
- [ ] Background worker processes jobs without errors
- [ ] Recommendations endpoint returns vector-enhanced matches
- [ ] Graceful fallback when Ollama/Qdrant unavailable
- [ ] Combined scoring produces better results than heuristic alone

## Dependencies
- gRPC client in Matching (issue #4) — for fetching jobs
- Fix job fetch trigger (issue #6) — need jobs to embed
EOF
)"

echo "Created issue: AI/Vector Matching"

# --- Issue 8: Frontend Recommendations ---
gh issue create \
  --title "feat: Add Recommendations page to frontend" \
  --label "enhancement,mvp,frontend" \
  --body "$(cat <<'EOF'
## Summary
Create a frontend page that displays AI-matched job recommendations from the Matching service.

## Scope

### New files
- `frontend/src/api/matching.ts` — API client calling `/api/matching/recommendations`
- `frontend/src/stores/matchingStore.ts` — Zustand store for recommendations state
- `frontend/src/pages/RecommendationsPage.tsx` — main page with job list
- `frontend/src/components/matching/MatchCard.tsx` — job card showing match score, company, location, salary
- `frontend/src/components/matching/ScoreBreakdown.tsx` — visual breakdown of scoring factors (skills, title, location, salary, experience)

### Modified files
- Router config — add `/recommendations` protected route
- Navbar — add "Rekommendationer" nav link

## Design
- Card-based layout showing matched jobs sorted by score
- Each card shows: job title, company, location, match score (percentage), top matching factors
- Click card to expand/navigate to full job details
- Option to save job from the card
- Loading state while recommendations are being computed

## Acceptance Criteria
- [ ] Recommendations page accessible at `/recommendations` (authenticated only)
- [ ] Calls `/api/matching/recommendations` via gateway
- [ ] Displays matched jobs with scores and factor breakdown
- [ ] Can save a job from the recommendations view
- [ ] Shows meaningful empty state if no profile or no matches
- [ ] Nav link visible in sidebar/navbar
- [ ] Swedish language labels consistent with existing UI

## Dependencies
- Vector matching in backend (issue #7) — for meaningful results
EOF
)"

echo "Created issue: Frontend Recommendations"

# --- Issue 9: Docker Compose Full Stack ---
gh issue create \
  --title "feat: Add all application services to docker-compose" \
  --label "enhancement,mvp,devops" \
  --body "$(cat <<'EOF'
## Summary
The current docker-compose.yml only starts infrastructure services (PostgreSQL, MongoDB, RabbitMQ, MinIO, Qdrant, Ollama). Add all application services for full-stack local deployment.

## Scope

### Modified files
- `deploy/docker/docker-compose.yml`

### Services to add
| Service | HTTP Port | gRPC Port | Depends On |
|---------|-----------|-----------|------------|
| Identity | 5001 | — | PostgreSQL |
| Profile | 5002 | 5012 | PostgreSQL, MinIO |
| Jobs | 5003 | 5013 | PostgreSQL, MongoDB |
| Matching | 5005 | — | Profile (gRPC), Jobs (gRPC), Qdrant, Ollama |
| Notifications | 5006 | — | PostgreSQL, RabbitMQ, Profile (gRPC) |
| Gateway | 5000 | — | All services |
| Frontend | 5173/80 | — | Gateway |

### For each service
- Multi-stage Dockerfile build
- Environment variables for connection strings (no secrets in compose)
- Health check configuration
- Dependency ordering with `depends_on` + health checks
- Volume mounts for development (optional)

### Verify/update
- All existing Dockerfiles support gRPC port exposure
- Kestrel configured for correct ports in containers

## Acceptance Criteria
- [ ] `docker compose up` starts the entire stack
- [ ] All services pass health checks
- [ ] Frontend accessible at localhost:5173
- [ ] Can register, login, and use the app end-to-end
- [ ] Database migrations run on startup (or via init containers)
EOF
)"

echo "Created issue: Docker Compose Full Stack"

# --- Issue 10: K8s / Homelab Deployment ---
gh issue create \
  --title "feat: K8s/Helm deployment for homelab k3s cluster" \
  --label "enhancement,mvp,devops" \
  --body "$(cat <<'EOF'
## Summary
Update Helm charts and Kubernetes manifests to deploy the complete JobRecon stack to a homelab k3s cluster.

## Scope

### Helm chart updates (`deploy/helm/jobrecon/`)
- `values.yaml` — add Jobs, Matching, Notifications service definitions with gRPC ports
- `templates/` — service templates with dual ports (HTTP + gRPC where needed)
- ConfigMaps for appsettings overrides
- Ingress rules for traefik (api.jobrecon.local, jobrecon.local)

### K8s manifests (`deploy/k8s/`)
- `base/` — add missing Deployments + Services for Jobs, Matching, Notifications
- `overlays/homelab/` — homelab-specific config (resource limits, node affinity)

### Kubernetes Secrets
- DB passwords (PostgreSQL)
- JWT signing key
- SMTP credentials
- MinIO credentials
- RabbitMQ credentials

### Infrastructure in k3s
- PostgreSQL (single instance, multiple schemas: identity, profile, jobs, notifications)
- MongoDB
- RabbitMQ
- MinIO
- Qdrant
- Ollama (requires sufficient CPU/RAM — or GPU if available)

### Database migrations
- Init container or Kubernetes Job to run EF Core migrations on deploy

## Acceptance Criteria
- [ ] `helm install jobrecon deploy/helm/jobrecon/` deploys successfully
- [ ] All pods healthy and running
- [ ] Frontend accessible via ingress at jobrecon.local
- [ ] API accessible via ingress at api.jobrecon.local
- [ ] gRPC communication works between pods (ClusterIP services)
- [ ] Secrets not exposed in any manifest
- [ ] Ollama model pulled and available
- [ ] End-to-end flow works: register → profile → jobs → recommendations

## Dependencies
- All previous issues completed
- k3s cluster accessible
- Container registry available (ghcr.io or local registry)
EOF
)"

echo "Created issue: K8s/Helm Deployment"

echo ""
echo "All 10 MVP issues created successfully!"
echo "View them at: https://github.com/$REPO/issues"
