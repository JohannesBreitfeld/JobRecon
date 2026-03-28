# Plan: MVP — End-to-End Job Recommendations

## Context

All 5 services are built but never tested as a system. The goal is a personal MVP where:
1. Jobs are fetched from JobTech Links (Swedish jobs)
2. User creates a profile with skills and preferences
3. AI-powered matching (Ollama + Qdrant) recommends relevant jobs
4. Everything runs in k3s on the user's homelab

Key architectural changes: replace inter-service HTTP calls with gRPC.

---

## Current State

| Service | Code Status | Gaps for MVP |
|---------|-------------|--------------|
| Identity (5001) | Ready | None |
| Profile (5002) | Ready | No service-to-service API (only serves authenticated user) |
| Jobs (5003) | Ready | Manual fetch trigger commented out |
| Matching (5005) | Partial | No Ollama/Qdrant, heuristic-only, HTTP clients to replace |
| Notifications (5006) | Ready | HTTP client to Profile needs gRPC, calls missing endpoint |
| Gateway (5000) | Ready | All routes configured |
| Frontend (5173) | Partial | No recommendations page |

### Inter-Service HTTP Calls → gRPC

| Caller | Target | Current HTTP Call | Data Exchanged |
|--------|--------|-------------------|----------------|
| Matching | Profile | `GET /api/profile/{userId}` | Full profile + skills + preferences |
| Matching | Jobs | `GET /api/jobs?page&size&status` | Paginated active jobs |
| Matching | Jobs | `GET /api/jobs/{jobId}` | Single job details |
| Notifications | Profile | `GET /api/profile/{userId}/email` | Email + display name (**endpoint doesn't exist**) |

**RabbitMQ messaging (Matching → Notifications):** Keep as-is — event-driven, not request-response.

---

## Implementation Steps

### Step 1: Define Proto Contracts

Create shared `.proto` files for service-to-service communication.

**New directory:** `src/Shared/JobRecon.Protos/`

**`profile_service.proto`** — Profile gRPC service:
```protobuf
service ProfileGrpc {
  rpc GetProfile (GetProfileRequest) returns (ProfileResponse);
  rpc GetUserEmail (GetUserEmailRequest) returns (UserEmailResponse);
}
```

**`jobs_service.proto`** — Jobs gRPC service:
```protobuf
service JobsGrpc {
  rpc GetActiveJobs (GetActiveJobsRequest) returns (JobListResponse);
  rpc GetJob (GetJobRequest) returns (JobResponse);
}
```

**New project:** `JobRecon.Protos.csproj` — shared proto project referenced by servers and clients.

---

### Step 2: Add gRPC Server to Profile Service

Implement `ProfileGrpc` service in the Profile service.

**Files to create:**
- `src/Services/JobRecon.Profile/Grpc/ProfileGrpcService.cs`

**Files to modify:**
- `src/Services/JobRecon.Profile/JobRecon.Profile.csproj` — add `Grpc.AspNetCore`, reference Protos
- `src/Services/JobRecon.Profile/Program.cs` — register gRPC, map service
- `src/Services/JobRecon.Profile/appsettings.json` — configure Kestrel for HTTP/2 (gRPC port)

The gRPC service reads directly from the Profile DbContext (no JWT needed for internal calls).

---

### Step 3: Add gRPC Server to Jobs Service

Implement `JobsGrpc` service in the Jobs service.

**Files to create:**
- `src/Services/JobRecon.Jobs/Grpc/JobsGrpcService.cs`

**Files to modify:**
- `src/Services/JobRecon.Jobs/JobRecon.Jobs.csproj` — add `Grpc.AspNetCore`, reference Protos
- `src/Services/JobRecon.Jobs/Program.cs` — register gRPC, map service
- `src/Services/JobRecon.Jobs/appsettings.json` — configure Kestrel for HTTP/2

---

### Step 4: Replace HTTP Clients with gRPC in Matching Service

**Files to modify:**
- `src/Services/JobRecon.Matching/Services/ProfileClient.cs` → rewrite using gRPC
- `src/Services/JobRecon.Matching/Services/JobsClient.cs` → rewrite using gRPC
- `src/Services/JobRecon.Matching/JobRecon.Matching.csproj` — add `Grpc.Net.Client`, reference Protos
- `src/Services/JobRecon.Matching/Extensions/ServiceCollectionExtensions.cs` — replace HttpClient registrations with gRPC channel
- `src/Services/JobRecon.Matching/appsettings.json` — replace ServiceUrls with gRPC addresses

---

### Step 5: Replace HTTP Client with gRPC in Notifications Service

**Files to modify:**
- `src/Services/JobRecon.Notifications/Contracts/ProfileClient.cs` → rewrite using gRPC
- `src/Services/JobRecon.Notifications/JobRecon.Notifications.csproj` — add `Grpc.Net.Client`, reference Protos
- `src/Services/JobRecon.Notifications/Extensions/ServiceCollectionExtensions.cs` — replace HttpClient with gRPC channel
- `src/Services/JobRecon.Notifications/appsettings.json` — add gRPC address config

---

### Step 6: Fix Jobs Service — Manual Fetch Trigger

**File to modify:**
- `src/Services/JobRecon.Jobs/Services/JobSourceService.cs` (~line 202) — uncomment/implement manual trigger

---

### Step 7: Add AI/Vector Matching to Matching Service

#### 7a: Ollama Embedding Client
Generate embeddings using `nomic-embed-text` via Ollama's HTTP API.

**New files:**
- `src/Services/JobRecon.Matching/Clients/IOllamaClient.cs`
- `src/Services/JobRecon.Matching/Clients/OllamaClient.cs`

#### 7b: Qdrant Vector Store
Store and query job embeddings.

**New files:**
- `src/Services/JobRecon.Matching/Clients/IVectorStore.cs`
- `src/Services/JobRecon.Matching/Clients/QdrantVectorStore.cs`

**New NuGet:** `Qdrant.Client`

#### 7c: Job Embedding Background Worker
Periodically embeds un-embedded jobs via Ollama → Qdrant.

**New file:**
- `src/Services/JobRecon.Matching/Workers/JobEmbeddingWorker.cs`

#### 7d: Update Matching Pipeline
1. Generate profile embedding
2. Query Qdrant for top-100 similar jobs (vector pre-filter)
3. Fetch full job details via gRPC
4. Apply heuristic scoring on shortlist
5. Return combined score (50% vector + 50% heuristic)
6. Fallback to heuristic-only if Qdrant empty

**Files to modify:**
- `src/Services/JobRecon.Matching/Services/MatchingService.cs`
- `src/Services/JobRecon.Matching/Program.cs`
- `src/Services/JobRecon.Matching/appsettings.json` — add Ollama + Qdrant config

---

### Step 8: Frontend — Recommendations Page

**New files:**
- `frontend/src/api/matching.ts`
- `frontend/src/stores/matchingStore.ts`
- `frontend/src/pages/RecommendationsPage.tsx`
- `frontend/src/components/matching/MatchCard.tsx`
- `frontend/src/components/matching/ScoreBreakdown.tsx`

**Files to modify:**
- Router config — add `/recommendations` route
- Navbar — add nav link

---

### Step 9: Containerize All Services

**Files to verify/update:**
- All Dockerfiles (already exist but may need gRPC port exposure)
- `deploy/docker/docker-compose.yml` — add all 5 services + gateway + frontend

Configure Kestrel to expose both HTTP (REST/gateway) and HTTP/2 (gRPC/internal) ports per service:
- Identity: 5001 (HTTP)
- Profile: 5002 (HTTP), 5012 (gRPC)
- Jobs: 5003 (HTTP), 5013 (gRPC)
- Matching: 5005 (HTTP), 5015 (gRPC)
- Notifications: 5006 (HTTP)
- Gateway: 5000 (HTTP)

---

### Step 10: K8s / Homelab Deployment

**Files to update:**
- `deploy/helm/jobrecon/values.yaml` — add Jobs, Matching, Notifications, gRPC ports
- `deploy/helm/jobrecon/templates/` — service templates with dual ports (HTTP + gRPC)
- `deploy/k8s/base/` — add missing service deployments (Jobs, Matching, Notifications)
- `deploy/k8s/overlays/` — homelab-specific config
- Add Kubernetes Secrets for: DB passwords, JWT signing key, SMTP credentials, MinIO credentials

**Infrastructure in k3s:**
- PostgreSQL (3 databases: identity, profile, jobs — or use one instance with schemas)
- MongoDB (jobs storage)
- RabbitMQ
- MinIO
- Qdrant
- Ollama (needs GPU or sufficient CPU/RAM)

---

## End-to-End Test Flow

1. Deploy infrastructure to k3s
2. Deploy all services
3. Pull Ollama model: `ollama pull nomic-embed-text`
4. Run DB migrations (init containers or jobs)
5. Access frontend via ingress
6. Register user account
7. Create profile: add skills ("C#", ".NET", "React"), desired titles ("Backend Developer"), preferences
8. Trigger or wait for JobTech Links fetch
9. Wait for embedding worker to process jobs → Qdrant
10. Navigate to Recommendations page
11. See AI-matched jobs with scores

---

## Priority Order

| # | Step | Effort | Notes |
|---|------|--------|-------|
| 1 | Proto contracts | 1 hr | Foundation for gRPC |
| 2 | gRPC server — Profile | 2 hr | |
| 3 | gRPC server — Jobs | 2 hr | Can parallel with Step 2 |
| 4 | gRPC client — Matching | 1 hr | Replace HTTP calls |
| 5 | gRPC client — Notifications | 30 min | Replace HTTP call |
| 6 | Fix job fetch trigger | 15 min | |
| 7 | Vector matching (Ollama + Qdrant) | 4-6 hr | Core MVP feature |
| 8 | Frontend recommendations page | 2-3 hr | |
| 9 | Docker compose full stack | 1-2 hr | Local testing |
| 10 | K8s/Helm deployment | 3-4 hr | Homelab |

---

## Out of Scope for MVP
- CV parsing (add skills manually)
- Notifications UI (check recommendations manually)
- Event-driven matching (on-demand is fine)
- Additional job sources
- Application tracking
