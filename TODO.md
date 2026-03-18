# JobRecon - Development Task List

> Track progress by checking boxes: `- [x]` = done, `- [ ]` = pending

---

## Phase 0: Project Foundation ✅

### Infrastructure Setup
- [x] Create solution structure (JobRecon.slnx)
- [x] Create .gitignore with security-focused exclusions
- [x] Create CLAUDE.md project guidelines
- [x] Create .editorconfig
- [x] Create Directory.Build.props with analyzers
- [x] Create .pre-commit-config.yaml
- [x] Create .secrets.baseline for detect-secrets

### Shared Libraries
- [x] Create JobRecon.Contracts project
- [x] Create JobRecon.Domain project
- [x] Create JobRecon.Infrastructure project

### Service Scaffolds
- [x] Create JobRecon.Identity service project
- [x] Create JobRecon.Profile service project
- [x] Create JobRecon.Gateway project (YARP)

### Frontend Scaffold
- [x] Create React + TypeScript + Vite project
- [x] Configure MUI theme
- [x] Configure TanStack Query
- [x] Configure React Router
- [x] Configure Zustand
- [x] Configure Vitest for testing
- [x] Configure ESLint

### Deployment Infrastructure
- [x] Create docker-compose.yml for local infrastructure
- [x] Create .env.example
- [x] Create Helm chart structure
- [x] Create Helm templates for all services
- [x] Create Kustomize base manifests
- [x] Create Kustomize overlays (local, production)
- [x] Create ArgoCD application manifest
- [x] Create Dockerfiles for all services

### CI/CD
- [x] Create GitHub Actions CI workflow
- [x] Create GitHub Actions Docker build workflow
- [x] Create GitHub Actions migrations workflow
- [x] Create Dependabot configuration
- [x] Create CODEOWNERS file

---

## Phase 1: Identity & Core Infrastructure

### Identity Service - Domain
- [x] Create User entity
- [x] Create RefreshToken entity
- [x] Create ExternalLogin entity
- [x] Create Role and UserRole entities
- [x] Define domain events (UserRegistered, PasswordChanged, etc.)

### Identity Service - Infrastructure
- [x] Configure ASP.NET Core Identity
- [x] Create Identity DbContext
- [x] Create EF Core migrations
- [x] Implement JWT token generation
- [x] Implement refresh token rotation
- [x] Configure password policies

### Identity Service - API
- [x] POST /api/auth/register
- [x] POST /api/auth/login
- [x] POST /api/auth/refresh
- [x] POST /api/auth/logout
- [ ] POST /api/auth/forgot-password
- [ ] POST /api/auth/reset-password
- [ ] GET /api/auth/verify-email
- [x] Configure NSwag/OpenAPI
- [x] Add health check endpoints

### Identity Service - Tests
- [x] Unit tests for token generation
- [x] Unit tests for password validation
- [x] Unit tests for RefreshToken domain entity
- [ ] Integration tests for auth endpoints
- [ ] Integration tests for refresh token flow

### API Gateway
- [x] Configure YARP routes
- [x] Configure JWT validation
- [x] Configure rate limiting
- [x] Configure CORS
- [x] Aggregate OpenAPI specs
- [x] Add health check endpoints

### Frontend - Authentication
- [x] Create auth API client
- [x] Create auth store (Zustand)
- [x] Create Login page
- [x] Create Register page
- [ ] Create Forgot Password page
- [x] Create Layout with Navbar
- [x] Implement token refresh interceptor
- [x] Create protected route wrapper
- [ ] Create role-based auth guards

### Frontend - Authentication Tests
- [x] Unit tests for auth store
- [x] Unit tests for LoginForm
- [x] Unit tests for RegisterForm

---

## Phase 2: Profile Service

### Profile Service - Domain
- [ ] Create UserProfile entity
- [ ] Create Skill entity
- [ ] Create JobPreference entity
- [ ] Create CVDocument entity
- [ ] Define domain events (ProfileUpdated, CVUploaded, etc.)

### Profile Service - Infrastructure
- [ ] Create Profile DbContext
- [ ] Create EF Core migrations
- [ ] Configure MinIO/S3 client for CV storage
- [ ] Implement file upload service
- [ ] Configure MassTransit

### Profile Service - API
- [ ] GET /api/profiles/me
- [ ] PUT /api/profiles/me
- [ ] POST /api/profiles/me/skills
- [ ] DELETE /api/profiles/me/skills/{id}
- [ ] GET /api/profiles/me/preferences
- [ ] PUT /api/profiles/me/preferences
- [ ] POST /api/profiles/me/cv
- [ ] GET /api/profiles/me/cv
- [ ] DELETE /api/profiles/me/cv/{id}
- [ ] Configure NSwag/OpenAPI
- [ ] Add health check endpoints

### Profile Service - Events
- [ ] Publish ProfileUpdated event
- [ ] Publish CVUploaded event
- [ ] Publish PreferencesChanged event

### Profile Service - Tests
- [ ] Unit tests for profile validation
- [ ] Unit tests for skill management
- [ ] Integration tests for profile CRUD
- [ ] Integration tests for CV upload

### Frontend - Profile
- [ ] Create profile API client
- [ ] Create profile store
- [ ] Create Profile page
- [ ] Create Edit Profile form
- [ ] Create Skills management component
- [ ] Create Job Preferences form
- [ ] Create CV upload component
- [ ] Create CV viewer component

---

## Phase 3: Job Crawler System

### Crawler Orchestrator - Domain
- [ ] Create CrawlerSource entity
- [ ] Create CrawlJob entity
- [ ] Create CrawlSchedule entity
- [ ] Define crawler configuration model

### Crawler Orchestrator - Infrastructure
- [ ] Create Crawler DbContext
- [ ] Create EF Core migrations
- [ ] Configure Quartz.NET scheduler
- [ ] Implement scheduler service

### Crawler Orchestrator - API (Admin)
- [ ] GET /api/admin/sources
- [ ] POST /api/admin/sources
- [ ] PUT /api/admin/sources/{id}
- [ ] DELETE /api/admin/sources/{id}
- [ ] GET /api/admin/jobs
- [ ] POST /api/admin/jobs/{id}/trigger
- [ ] GET /api/admin/jobs/{id}/status

### Crawler Workers
- [ ] Create worker project
- [ ] Implement HTTP fetcher with Polly
- [ ] Implement HTML parser (AngleSharp)
- [ ] Create extractor strategy pattern
- [ ] Implement Indeed extractor
- [ ] Implement LinkedIn extractor (if possible)
- [ ] Implement generic job board extractor
- [ ] Publish RawJobDiscovered events

### Crawler - Tests
- [ ] Unit tests for extractors
- [ ] Unit tests for scheduler
- [ ] Integration tests with mock HTML

---

## Phase 4: Job Processing & Storage

### Job Processor Service - Domain
- [ ] Create Job entity (MongoDB document)
- [ ] Create JobSource entity
- [ ] Define normalization rules
- [ ] Define deduplication strategy

### Job Processor Service - Infrastructure
- [ ] Configure MongoDB client
- [ ] Create MongoDB indexes
- [ ] Implement content hash generation
- [ ] Implement fuzzy matching for deduplication

### Job Processor Service - Consumers
- [ ] Consume RawJobDiscovered events
- [ ] Implement normalization pipeline
- [ ] Implement salary parsing
- [ ] Implement location geocoding
- [ ] Publish JobNormalized events
- [ ] Publish JobDeduplicated events

### Job Processor - Tests
- [ ] Unit tests for normalization
- [ ] Unit tests for deduplication
- [ ] Unit tests for salary parsing
- [ ] Integration tests with MongoDB

---

## Phase 5: AI Pipeline

### AI Pipeline Service - Infrastructure
- [ ] Configure Ollama client
- [ ] Create embedding generation service
- [ ] Create CV parsing service
- [ ] Implement batch processing queue
- [ ] Configure model management

### AI Pipeline Service - Consumers
- [ ] Consume CVUploaded events
- [ ] Consume JobNormalized events
- [ ] Consume ProfileUpdated events
- [ ] Generate embeddings (nomic-embed-text)
- [ ] Parse CV content (Mistral 7B)
- [ ] Publish EmbeddingGenerated events
- [ ] Publish CVParsed events

### AI Pipeline - Tests
- [ ] Unit tests for embedding service
- [ ] Unit tests for CV parser
- [ ] Integration tests with Ollama

---

## Phase 6: Vector Search & Matching

### Vector Indexer Service
- [ ] Configure Qdrant client
- [ ] Create collection schemas
- [ ] Implement vector ingestion
- [ ] Implement hybrid search
- [ ] Consume EmbeddingGenerated events

### Match Evaluator Service - Domain
- [ ] Create Match entity
- [ ] Create MatchScore entity
- [ ] Define scoring algorithm

### Match Evaluator Service - Infrastructure
- [ ] Create Match DbContext
- [ ] Configure Qdrant client
- [ ] Configure Ollama client for LLM evaluation

### Match Evaluator Service - Logic
- [ ] Implement vector similarity search
- [ ] Implement metadata filtering
- [ ] Implement LLM-based evaluation
- [ ] Implement score aggregation
- [ ] Implement match persistence

### Match Evaluator - Consumers
- [ ] Consume JobNormalized events
- [ ] Consume ProfileUpdated events
- [ ] Publish MatchFound events
- [ ] Publish MatchScoreUpdated events

### Match Evaluator - Tests
- [ ] Unit tests for scoring algorithm
- [ ] Unit tests for filtering
- [ ] Integration tests with Qdrant

---

## Phase 7: Job Query Service (CQRS Read Side)

### Job Query Service - Infrastructure
- [ ] Create read model projections
- [ ] Configure full-text search
- [ ] Implement faceted filtering
- [ ] Implement pagination

### Job Query Service - API
- [ ] GET /api/jobs (search with filters)
- [ ] GET /api/jobs/{id}
- [ ] GET /api/jobs/recommended
- [ ] GET /api/matches
- [ ] GET /api/matches/{id}
- [ ] Configure NSwag/OpenAPI

### Job Query - Tests
- [ ] Unit tests for search logic
- [ ] Integration tests for API

### Frontend - Jobs
- [ ] Create jobs API client
- [ ] Create jobs store
- [ ] Create Job Search page
- [ ] Create Job filters component
- [ ] Create Job card component
- [ ] Create Job detail page
- [ ] Create Matches page
- [ ] Create Match card component

---

## Phase 8: Notifications

### Notification Service - Domain
- [ ] Create Notification entity
- [ ] Create NotificationTemplate entity
- [ ] Create NotificationPreference entity

### Notification Service - Infrastructure
- [ ] Create Notification DbContext
- [ ] Configure email provider (SMTP/SendGrid)
- [ ] Implement Razor email templates
- [ ] Implement delivery tracking

### Notification Service - Consumers
- [ ] Consume MatchFound events
- [ ] Implement immediate notifications
- [ ] Implement digest batching
- [ ] Implement notification preferences

### Notification Service - API
- [ ] GET /api/notifications
- [ ] PUT /api/notifications/{id}/read
- [ ] GET /api/notifications/preferences
- [ ] PUT /api/notifications/preferences

### Notification - Tests
- [ ] Unit tests for template rendering
- [ ] Unit tests for batching logic
- [ ] Integration tests with mock SMTP

### Frontend - Notifications
- [ ] Create notifications API client
- [ ] Create notifications store
- [ ] Create Notifications dropdown
- [ ] Create Notifications page
- [ ] Create Notification preferences form

---

## Phase 9: Observability & Operations

### Logging
- [ ] Configure Serilog for all services
- [ ] Configure structured logging
- [ ] Add correlation IDs
- [ ] Configure log aggregation (Loki/ELK)

### Metrics
- [ ] Add Prometheus metrics endpoints
- [ ] Configure custom business metrics
- [ ] Create Grafana dashboards

### Tracing
- [ ] Configure OpenTelemetry
- [ ] Add distributed tracing
- [ ] Configure Jaeger/Zipkin

### Health Checks
- [ ] Add liveness probes
- [ ] Add readiness probes
- [ ] Add dependency health checks
- [ ] Configure health check UI

---

## Phase 10: Security Hardening

### API Security
- [ ] Implement API versioning
- [ ] Add request validation
- [ ] Add response sanitization
- [ ] Configure security headers
- [ ] Implement audit logging

### Data Security
- [ ] Encrypt sensitive data at rest
- [ ] Implement field-level encryption for PII
- [ ] Configure TLS for all internal communication
- [ ] Implement data retention policies

### External Identity Providers
- [ ] Implement Google OAuth
- [ ] Implement GitHub OAuth
- [ ] Implement EntraID integration
- [ ] Implement account linking

---

## Phase 11: Performance & Scaling

### Caching
- [ ] Implement Redis caching layer
- [ ] Add cache invalidation logic
- [ ] Cache frequently accessed data

### Database Optimization
- [ ] Add database indexes
- [ ] Implement read replicas
- [ ] Configure connection pooling
- [ ] Implement query optimization

### Load Testing
- [ ] Create k6 load test scripts
- [ ] Establish performance baselines
- [ ] Identify bottlenecks
- [ ] Document scaling recommendations

---

## Phase 12: Production Readiness

### Documentation
- [ ] Complete API documentation
- [ ] Create deployment guide
- [ ] Create operations runbook
- [ ] Document troubleshooting procedures

### Azure Deployment
- [ ] Create Azure resource templates (Bicep/Terraform)
- [ ] Configure Azure Container Apps / AKS
- [ ] Configure Azure Service Bus
- [ ] Configure Azure Key Vault
- [ ] Configure Azure Monitor

### Disaster Recovery
- [ ] Implement backup strategies
- [ ] Document recovery procedures
- [ ] Test failover scenarios

---

## Backlog / Future Enhancements

### Features
- [ ] Real-time notifications (SignalR/WebSockets)
- [ ] Job application tracking
- [ ] Interview scheduling
- [ ] Company profiles and reviews
- [ ] Salary insights
- [ ] Career path recommendations

### Technical Improvements
- [ ] GraphQL API option
- [ ] gRPC for internal services
- [ ] Event replay capability
- [ ] A/B testing framework
- [ ] Feature flags system

---

## Progress Summary

| Phase | Status | Progress |
|-------|--------|----------|
| Phase 0: Foundation | ✅ Complete | 100% |
| Phase 1: Identity | 🔄 In Progress | 85% |
| Phase 2: Profile | 🔲 Not Started | 0% |
| Phase 3: Crawler | 🔲 Not Started | 0% |
| Phase 4: Job Processing | 🔲 Not Started | 0% |
| Phase 5: AI Pipeline | 🔲 Not Started | 0% |
| Phase 6: Matching | 🔲 Not Started | 0% |
| Phase 7: Job Query | 🔲 Not Started | 0% |
| Phase 8: Notifications | 🔲 Not Started | 0% |
| Phase 9: Observability | 🔲 Not Started | 0% |
| Phase 10: Security | 🔲 Not Started | 0% |
| Phase 11: Performance | 🔲 Not Started | 0% |
| Phase 12: Production | 🔲 Not Started | 0% |

---

*Last updated: 2026-03-18*
