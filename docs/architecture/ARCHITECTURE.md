# JobRecon - Architecture Design Document

## 1. System Overview

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              FRONTEND LAYER                                      │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │              React + TypeScript SPA (Vite)                               │    │
│  │              OpenAPI-generated API clients                               │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              API GATEWAY                                         │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │           YARP Reverse Proxy (Rate Limiting, Auth, OpenAPI Aggregation) │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────────────┘
                                      │
        ┌─────────────┬───────────────┼───────────────┬─────────────┐
        ▼             ▼               ▼               ▼             ▼
┌──────────────┐┌──────────────┐┌──────────────┐┌──────────────┐┌──────────────┐
│   Identity   ││   Profile    ││  Job Query   ││    Match     ││ Notification │
│   Service    ││   Service    ││   Service    ││   Service    ││   Service    │
│  (OpenAPI)   ││  (OpenAPI)   ││  (OpenAPI)   ││  (OpenAPI)   ││  (OpenAPI)   │
└──────────────┘└──────────────┘└──────────────┘└──────────────┘└──────────────┘
        │             │               │               │             │
        └─────────────┴───────────────┼───────────────┴─────────────┘
                                      │
┌─────────────────────────────────────────────────────────────────────────────────┐
│                          MESSAGE BUS LAYER                                       │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │         RabbitMQ (Local) / Azure Service Bus (Cloud)                    │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────────────┘
                                      │
        ┌─────────────┬───────────────┼───────────────┬─────────────┐
        ▼             ▼               ▼               ▼             ▼
┌──────────────┐┌──────────────┐┌──────────────┐┌──────────────┐┌──────────────┐
│  Crawler     ││    Job       ││     AI       ││   Vector     ││   Match      │
│  Orchestrator││  Processor   ││  Pipeline    ││   Indexer    ││  Evaluator   │
└──────────────┘└──────────────┘└──────────────┘└──────────────┘└──────────────┘
                                      │
┌─────────────────────────────────────────────────────────────────────────────────┐
│                          DATA LAYER                                              │
│  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐    │
│  │ PostgreSQL │ │   Qdrant   │ │   Redis    │ │   MinIO    │ │  MongoDB   │    │
│  │  (Primary) │ │  (Vectors) │ │  (Cache)   │ │  (Blobs)   │ │  (Jobs)    │    │
│  └────────────┘ └────────────┘ └────────────┘ └────────────┘ └────────────┘    │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Architectural Principles

| Principle | Application |
|-----------|-------------|
| **Domain-Driven Design** | Bounded contexts for Identity, Profile, Jobs, Matching, Notifications |
| **Event Sourcing** | Job discovery and matching events stored for audit/replay |
| **CQRS** | Separate read/write models for job queries vs job ingestion |
| **Saga Pattern** | Orchestrated workflows for CV processing and match generation |
| **Sidecar Pattern** | Observability agents deployed alongside services |
| **Strangler Fig** | Allows incremental migration from local to cloud |
| **API-First** | OpenAPI specs drive client generation and documentation |

---

## 2. Proposed Microservices and Responsibilities

### 2.1 Identity Service

**Bounded Context:** User Authentication & Authorization

| Responsibility | Details |
|----------------|---------|
| User registration | Email/password with ASP.NET Core Identity |
| Authentication | Manual JWT Bearer token issuance |
| Refresh tokens | Secure rotation, stored in database |
| External IdP integration | EntraID, Google, GitHub OAuth (future) |
| Authorization | Role-based access (User, Admin) |
| Account management | Password reset, email verification |

**API Style:** REST (Minimal API) with NSwag
**Database:** PostgreSQL (ASP.NET Core Identity tables)
**Patterns:** JWT Bearer auth with refresh token rotation

**Identity Architecture:**

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     AUTHENTICATION FLOW                                  │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌─────────────────┐                                                    │
│  │  React Frontend │                                                    │
│  └────────┬────────┘                                                    │
│           │ 1. POST /api/auth/login (email, password)                   │
│           ▼                                                              │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │                    IDENTITY SERVICE                              │    │
│  │  ┌─────────────────┐    ┌─────────────────┐                     │    │
│  │  │  ASP.NET Core   │───▶│   JWT Token     │                     │    │
│  │  │    Identity     │    │   Generator     │                     │    │
│  │  │ (UserManager)   │    │                 │                     │    │
│  │  └─────────────────┘    └─────────────────┘                     │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│           │                                                              │
│           │ 2. Return { accessToken, refreshToken }                     │
│           ▼                                                              │
│  ┌─────────────────┐                                                    │
│  │  React Frontend │ Stores tokens, attaches to API requests           │
│  └─────────────────┘                                                    │
│                                                                          │
│  FUTURE: External providers (Google, GitHub, EntraID)                   │
│  via OAuth 2.0 authorization code flow                                  │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

### 2.2 Profile Service

**Bounded Context:** User Profile & Preferences

| Responsibility | Details |
|----------------|---------|
| Profile CRUD | Personal info, contact details |
| Skills management | Skill taxonomy, proficiency levels |
| CV storage | Upload, versioning, metadata |
| Job preferences | Location, salary range, remote preference, industries |
| Profile embeddings | Trigger embedding generation on profile changes |

**API Style:** REST + gRPC (internal), OpenAPI documented
**Database:** PostgreSQL (profiles), MinIO/Azure Blob (CV files)
**Events Published:** `ProfileUpdated`, `CVUploaded`, `PreferencesChanged`

---

### 2.3 Job Crawler Orchestrator

**Bounded Context:** Job Discovery & Scheduling

| Responsibility | Details |
|----------------|---------|
| Crawler scheduling | Quartz.NET-based job scheduling |
| Source management | CRUD for job board configurations |
| Crawler execution | Dispatch crawl jobs to workers |
| Rate limiting | Per-source request throttling |
| Health monitoring | Track crawler success/failure rates |

**API Style:** REST (admin), gRPC (internal)
**Database:** PostgreSQL (crawler configs, schedules)
**Events Published:** `CrawlJobScheduled`, `CrawlJobCompleted`, `CrawlJobFailed`

---

### 2.4 Job Crawler Workers

**Bounded Context:** Job Extraction (scaled horizontally)

| Responsibility | Details |
|----------------|---------|
| HTTP fetching | Resilient HTTP with Polly |
| HTML parsing | AngleSharp for DOM parsing |
| Data extraction | Site-specific extractors (strategy pattern) |
| Raw job emission | Publish raw job data to message bus |

**Scaling:** Multiple workers consuming from queue
**Events Published:** `RawJobDiscovered`

---

### 2.5 Job Processor Service

**Bounded Context:** Job Normalization & Storage

| Responsibility | Details |
|----------------|---------|
| Data normalization | Standardize job fields across sources |
| Deduplication | Content-hash + fuzzy matching |
| Enrichment | Extract structured data (salary parsing, location geocoding) |
| Storage | Persist normalized jobs |
| Expiration | Mark stale jobs as inactive |

**API Style:** gRPC (internal)
**Database:** MongoDB (job documents)
**Events Published:** `JobNormalized`, `JobDeduplicated`, `JobExpired`

---

### 2.6 AI Pipeline Service

**Bounded Context:** AI/ML Processing

| Responsibility | Details |
|----------------|---------|
| CV parsing | Extract structured data from uploaded CVs |
| Embedding generation | Generate embeddings for jobs and profiles |
| Model management | Ollama model lifecycle |
| Batch processing | Handle embedding backlog efficiently |

**Infrastructure:** Ollama sidecar, GPU access (local)
**Events Consumed:** `CVUploaded`, `JobNormalized`, `ProfileUpdated`
**Events Published:** `EmbeddingGenerated`, `CVParsed`

---

### 2.7 Vector Indexer Service

**Bounded Context:** Vector Storage & Search

| Responsibility | Details |
|----------------|---------|
| Vector ingestion | Store embeddings in Qdrant |
| Index management | Collection lifecycle, schema updates |
| Similarity search | Cosine similarity queries |
| Hybrid search | Combine vector + metadata filters |

**Database:** Qdrant
**API Style:** gRPC (internal)
**Events Consumed:** `EmbeddingGenerated`

---

### 2.8 Match Evaluator Service

**Bounded Context:** Job-Profile Matching

| Responsibility | Details |
|----------------|---------|
| Initial filtering | Vector similarity threshold check |
| Deep evaluation | LLM-based reasoning for top candidates |
| Score aggregation | Weighted scoring (skills, location, salary) |
| Match persistence | Store match results with explanations |

**Events Consumed:** `JobNormalized`, `ProfileUpdated`
**Events Published:** `MatchFound`, `MatchScoreUpdated`

---

### 2.9 Job Query Service (CQRS Read Side)

**Bounded Context:** Job Search & Discovery

| Responsibility | Details |
|----------------|---------|
| Full-text search | Elasticsearch/OpenSearch integration |
| Faceted filtering | By location, salary, skills, date |
| Personalized feed | User-specific job recommendations |
| Match retrieval | Get matches for a user |

**API Style:** REST (Minimal API) with OpenAPI
**Database:** Read replica + search index

---

### 2.10 Notification Service

**Bounded Context:** User Notifications

| Responsibility | Details |
|----------------|---------|
| Email dispatch | SMTP/SendGrid integration |
| Template management | Razor-based email templates |
| Notification preferences | Per-user channel config |
| Delivery tracking | Track sent/bounced/opened |
| Batching | Digest mode for multiple matches |

**Events Consumed:** `MatchFound`
**Database:** PostgreSQL (notification logs)

---

### 2.11 API Gateway

**Cross-Cutting Concern:** Ingress & Security

| Responsibility | Details |
|----------------|---------|
| Request routing | Route to appropriate service |
| Authentication | JWT validation (local + EntraID tokens) |
| Rate limiting | Per-user/IP throttling |
| OpenAPI aggregation | Unified API documentation |
| Request aggregation | BFF pattern for complex views |
| SSL termination | HTTPS handling |
| CORS | Frontend origin configuration |

**Technology:** YARP with Scalar/SwaggerUI for OpenAPI

---

## 3. Event-Driven Architecture and Message Flows

### 3.1 Event Categories

```
DOMAIN EVENTS
├── Profile Domain
│   ├── UserRegistered
│   ├── ProfileUpdated
│   ├── CVUploaded
│   ├── CVParsed
│   ├── PreferencesChanged
│   └── ProfileEmbeddingGenerated
│
├── Job Domain
│   ├── CrawlJobScheduled
│   ├── RawJobDiscovered
│   ├── JobNormalized
│   ├── JobDeduplicated
│   ├── JobEmbeddingGenerated
│   └── JobExpired
│
├── Matching Domain
│   ├── MatchingTriggered
│   ├── CandidateMatchFound
│   ├── MatchEvaluated
│   └── MatchScoreUpdated
│
└── Notification Domain
    ├── NotificationQueued
    ├── NotificationSent
    └── NotificationFailed
```

### 3.2 Core Message Flows

#### Flow 1: Job Discovery Pipeline

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│  Scheduler  │───▶│   Crawler   │───▶│  Processor  │───▶│ AI Pipeline │
│             │    │   Worker    │    │             │    │             │
└─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘
      │                  │                  │                  │
      │  CrawlJob        │  RawJob          │  JobNormalized   │  JobEmbedding
      │  Scheduled       │  Discovered      │                  │  Generated
      ▼                  ▼                  ▼                  ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                          MESSAGE BUS                                     │
└─────────────────────────────────────────────────────────────────────────┘
                                                                  │
                                                                  ▼
                                                         ┌─────────────┐
                                                         │   Vector    │
                                                         │   Indexer   │
                                                         └─────────────┘
```

#### Flow 2: Profile Update & Matching Trigger

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   Profile   │───▶│ AI Pipeline │───▶│   Vector    │───▶│   Match     │
│   Service   │    │             │    │   Indexer   │    │  Evaluator  │
└─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘
      │                  │                  │                  │
      │  ProfileUpdated  │  ProfileEmbed    │  Indexed         │  MatchFound
      │  CVUploaded      │  Generated       │                  │
      ▼                  ▼                  ▼                  ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                          MESSAGE BUS                                     │
└─────────────────────────────────────────────────────────────────────────┘
                                                                  │
                                                                  ▼
                                                         ┌─────────────┐
                                                         │Notification │
                                                         │  Service    │
                                                         └─────────────┘
```

#### Flow 3: Match Evaluation Saga

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        MATCH EVALUATION SAGA                             │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  1. MatchingTriggered (job or profile change)                           │
│         │                                                                │
│         ▼                                                                │
│  2. Vector Search (find top-N candidates)                               │
│         │                                                                │
│         ▼                                                                │
│  3. Filter by metadata (location, salary, remote)                       │
│         │                                                                │
│         ▼                                                                │
│  4. For each candidate above threshold:                                 │
│         │                                                                │
│         ├──▶ If score > LLM_THRESHOLD: Request LLM evaluation           │
│         │         │                                                      │
│         │         ▼                                                      │
│         │    LLM returns reasoning + confidence                         │
│         │                                                                │
│         └──▶ Else: Use vector score directly                            │
│                                                                          │
│  5. Aggregate scores → Publish MatchEvaluated                           │
│         │                                                                │
│         ▼                                                                │
│  6. If final_score > NOTIFICATION_THRESHOLD:                            │
│         Publish MatchFound → Notification Service                       │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### 3.3 Message Bus Topology

| Exchange/Topic | Type | Consumers | Purpose |
|---------------|------|-----------|---------|
| `job.discovery` | Fanout | Processor, Analytics | Raw job broadcasts |
| `job.normalized` | Topic | AI Pipeline, Search Indexer | Processed jobs |
| `profile.events` | Topic | AI Pipeline, Match Evaluator | Profile changes |
| `matching.candidates` | Direct | Match Evaluator | Candidate pairs |
| `notifications.outbound` | Direct | Notification Service | Notification dispatch |
| `dead.letter` | Direct | Dead Letter Handler | Failed messages |

---

## 4. Data Model Overview

### 4.1 PostgreSQL Schemas

#### Identity Schema

```
identity.users
├── id: UUID (PK)
├── email: VARCHAR(255) UNIQUE
├── password_hash: VARCHAR(255) NULLABLE (null for external IdP)
├── email_verified: BOOLEAN
├── created_at: TIMESTAMPTZ
├── updated_at: TIMESTAMPTZ
└── last_login_at: TIMESTAMPTZ

identity.external_logins
├── id: UUID (PK)
├── user_id: UUID (FK → users)
├── provider: VARCHAR(50) (entra, google, github)
├── provider_key: VARCHAR(255) (external user ID)
├── provider_display_name: VARCHAR(255)
└── created_at: TIMESTAMPTZ

identity.refresh_tokens
├── id: UUID (PK)
├── user_id: UUID (FK → users)
├── token_hash: VARCHAR(255)
├── expires_at: TIMESTAMPTZ
├── revoked: BOOLEAN
└── created_at: TIMESTAMPTZ
```

#### Profile Schema

```
profile.user_profiles
├── id: UUID (PK)
├── user_id: UUID (FK → identity.users)
├── first_name: VARCHAR(100)
├── last_name: VARCHAR(100)
├── headline: VARCHAR(255)
├── summary: TEXT
├── location: JSONB (city, country, coordinates)
├── years_of_experience: INTEGER
├── embedding_id: VARCHAR(255) (reference to Qdrant)
└── updated_at: TIMESTAMPTZ

profile.skills
├── id: UUID (PK)
├── profile_id: UUID (FK → user_profiles)
├── skill_name: VARCHAR(100)
├── proficiency: ENUM (beginner, intermediate, advanced, expert)
├── years: INTEGER
└── verified: BOOLEAN

profile.job_preferences
├── id: UUID (PK)
├── profile_id: UUID (FK → user_profiles)
├── min_salary: DECIMAL
├── max_salary: DECIMAL
├── currency: VARCHAR(3)
├── remote_preference: ENUM (remote, hybrid, onsite, any)
├── locations: JSONB (array of preferred locations)
├── industries: JSONB (array)
├── job_types: JSONB (full-time, contract, etc.)
├── match_threshold: DECIMAL (0.0-1.0)
└── notification_frequency: ENUM (immediate, daily, weekly)

profile.cv_documents
├── id: UUID (PK)
├── profile_id: UUID (FK → user_profiles)
├── filename: VARCHAR(255)
├── storage_path: VARCHAR(500)
├── content_hash: VARCHAR(64)
├── parsed_data: JSONB
├── version: INTEGER
├── uploaded_at: TIMESTAMPTZ
└── is_active: BOOLEAN
```

### 4.2 MongoDB Collections (Jobs)

#### jobs.raw_listings

```json
{
  "_id": "ObjectId",
  "source": "linkedin|indeed|glassdoor|...",
  "source_id": "external ID from source",
  "url": "original posting URL",
  "raw_html": "original HTML (compressed)",
  "raw_data": { /* source-specific structure */ },
  "crawled_at": "ISODate",
  "crawler_version": "1.0.0"
}
```

#### jobs.normalized_listings

```json
{
  "_id": "ObjectId",
  "source": "string",
  "source_id": "string",
  "content_hash": "SHA256 for dedup",

  "title": "Job Title",
  "company": {
    "name": "Company Name",
    "logo_url": "optional",
    "industry": "optional"
  },
  "location": {
    "city": "string",
    "state": "string",
    "country": "string",
    "coordinates": [lat, lng],
    "remote": true|false|"hybrid"
  },
  "salary": {
    "min": 80000,
    "max": 120000,
    "currency": "USD",
    "period": "yearly"
  },
  "description": "full job description",
  "requirements": ["array", "of", "requirements"],
  "extracted_skills": ["skill1", "skill2"],
  "job_type": "full-time|contract|part-time",
  "experience_level": "entry|mid|senior|lead",

  "embedding_id": "reference to Qdrant vector",

  "posted_at": "ISODate",
  "expires_at": "ISODate",
  "normalized_at": "ISODate",
  "status": "active|expired|removed"
}
```

### 4.3 Qdrant Collections

#### job_embeddings

```
Collection: job_embeddings
├── vector_size: 1536 (or model-specific)
├── distance: Cosine
│
└── Point Structure:
    ├── id: UUID (matches MongoDB job _id)
    ├── vector: [1536 floats]
    └── payload:
        ├── title: string
        ├── company: string
        ├── location_country: string
        ├── remote: boolean
        ├── salary_min: number
        ├── salary_max: number
        ├── job_type: string
        ├── experience_level: string
        ├── skills: array<string>
        └── posted_at: timestamp
```

#### profile_embeddings

```
Collection: profile_embeddings
├── vector_size: 1536
├── distance: Cosine
│
└── Point Structure:
    ├── id: UUID (matches PostgreSQL profile id)
    ├── vector: [1536 floats]
    └── payload:
        ├── user_id: UUID
        ├── years_experience: number
        ├── skills: array<string>
        ├── location_country: string
        └── updated_at: timestamp
```

### 4.4 Match Storage

```
matching.job_matches
├── id: UUID (PK)
├── profile_id: UUID (FK → profile.user_profiles)
├── job_id: VARCHAR(24) (MongoDB ObjectId reference)
├── vector_score: DECIMAL (cosine similarity)
├── llm_score: DECIMAL (optional)
├── llm_reasoning: TEXT (optional)
├── final_score: DECIMAL (weighted aggregate)
├── match_factors: JSONB (breakdown by category)
├── status: ENUM (new, viewed, saved, dismissed, applied)
├── created_at: TIMESTAMPTZ
├── notified_at: TIMESTAMPTZ
└── viewed_at: TIMESTAMPTZ
```

---

## 5. AI Pipeline Design

### 5.1 Pipeline Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        AI PIPELINE SERVICE                               │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐      │
│  │  CV Parser      │    │   Embedding     │    │  LLM Evaluator  │      │
│  │  Worker         │    │   Worker        │    │  Worker         │      │
│  └────────┬────────┘    └────────┬────────┘    └────────┬────────┘      │
│           │                      │                      │                │
│           ▼                      ▼                      ▼                │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │                    OLLAMA SIDECAR                                │    │
│  │  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐        │    │
│  │  │ nomic-embed   │  │ llama3.2      │  │ mistral       │        │    │
│  │  │ (embeddings)  │  │ (parsing)     │  │ (evaluation)  │        │    │
│  │  └───────────────┘  └───────────────┘  └───────────────┘        │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### 5.2 Model Selection

| Task | Model | Size | Reasoning |
|------|-------|------|-----------|
| **Embeddings** | nomic-embed-text | ~274MB | Good quality, fast, 8192 token context |
| **CV Parsing** | llama3.2:3b | ~2GB | Structured extraction, JSON mode |
| **Match Evaluation** | mistral:7b | ~4GB | Reasoning, explanation generation |
| **Fallback Embeddings** | all-minilm | ~45MB | Fast, good for high volume |

### 5.3 CV Parsing Pipeline

```
Input: PDF/DOCX file
         │
         ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ STEP 1: Document Extraction                                              │
│ ─────────────────────────────                                           │
│ • PDF: PdfPig library                                                   │
│ • DOCX: Open XML SDK                                                    │
│ • Output: Raw text + structural hints                                   │
└─────────────────────────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ STEP 2: LLM Structured Extraction                                        │
│ ─────────────────────────────────                                       │
│ • Prompt template for extraction                                        │
│ • JSON mode for structured output                                       │
│ • Extract: name, contact, experience, education, skills, certifications │
└─────────────────────────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ STEP 3: Skill Normalization                                              │
│ ────────────────────────────                                            │
│ • Map extracted skills to canonical taxonomy                            │
│ • Handle synonyms (JS → JavaScript, k8s → Kubernetes)                   │
│ • Assign confidence scores                                              │
└─────────────────────────────────────────────────────────────────────────┘
         │
         ▼
Output: ParsedCVDocument (stored in profile.cv_documents.parsed_data)
```

### 5.4 Embedding Generation

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     EMBEDDING STRATEGY                                   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  JOB EMBEDDING:                                                          │
│  ─────────────                                                          │
│  Text composition:                                                       │
│  "{title}. {company}. Requirements: {requirements_list}.                │
│   Skills: {skills_list}. {description_summary}"                         │
│                                                                          │
│  Truncation: 8000 tokens (nomic-embed context)                          │
│  Chunking: Single embedding per job (no chunking)                       │
│                                                                          │
│  ────────────────────────────────────────────────────────────────────   │
│                                                                          │
│  PROFILE EMBEDDING:                                                      │
│  ─────────────────                                                      │
│  Text composition:                                                       │
│  "{headline}. {summary}. Skills: {skills_with_proficiency}.             │
│   Experience: {years} years. {recent_job_titles}"                       │
│                                                                          │
│  CV content: Append parsed CV summary if available                      │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### 5.5 Match Evaluation Pipeline

```
                         ┌───────────────────────┐
                         │    New Job Indexed    │
                         │         OR            │
                         │  Profile Updated      │
                         └───────────┬───────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ STAGE 1: Vector Candidate Retrieval                                     │
│ ────────────────────────────────────                                    │
│ • Query Qdrant for top-K similar profiles/jobs                          │
│ • K = 100 (configurable)                                                │
│ • Filter by payload metadata (location, remote, salary range)          │
│ • Output: Candidate pairs with vector scores                            │
└─────────────────────────────────────────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ STAGE 2: Metadata Score Adjustment                                       │
│ ──────────────────────────────────                                      │
│ • Skill overlap: +0.1 per matching skill (max +0.3)                     │
│ • Location match: +0.15                                                 │
│ • Salary in range: +0.1                                                 │
│ • Experience level match: +0.1                                          │
│ • Remote preference match: +0.05                                        │
└─────────────────────────────────────────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ STAGE 3: Threshold Filtering                                             │
│ ────────────────────────────                                            │
│                                                                          │
│  adjusted_score < 0.5  ───────▶  DISCARD                                │
│                                                                          │
│  0.5 ≤ score < 0.75  ─────────▶  STORE (no LLM)                         │
│                                                                          │
│  score ≥ 0.75  ───────────────▶  PROCEED TO LLM EVALUATION              │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ STAGE 4: LLM Deep Evaluation (Top Candidates Only)                       │
│ ──────────────────────────────────────────────────                      │
│ • Construct evaluation prompt with job + profile details               │
│ • Request structured analysis:                                          │
│   - Match quality (1-10)                                                │
│   - Key strengths                                                       │
│   - Potential gaps                                                      │
│   - Reasoning summary                                                   │
│ • Rate limit: Max 50 LLM calls per user per day                         │
└─────────────────────────────────────────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ STAGE 5: Final Score & Persistence                                       │
│ ──────────────────────────────────                                      │
│ final_score = (vector_score × 0.4) + (metadata_score × 0.3)             │
│             + (llm_score × 0.3)                                         │
│                                                                          │
│ If final_score ≥ user.match_threshold → Publish MatchFound              │
└─────────────────────────────────────────────────────────────────────────┘
```

### 5.6 Resource Management

| Resource | Local Allocation | Notes |
|----------|------------------|-------|
| **GPU Memory** | 8GB recommended | Runs mistral:7b comfortably |
| **Model Loading** | Lazy load | Models loaded on first request |
| **Concurrent Requests** | 2 parallel | Prevents OOM |
| **Request Queue** | RabbitMQ backed | Handles bursts gracefully |
| **Timeout** | 60s embeddings, 120s LLM | Prevents hung requests |

---

## 6. Job Crawler Architecture

### 6.1 Crawler System Design

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     CRAWLER ORCHESTRATOR                                 │
├─────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │                    QUARTZ.NET SCHEDULER                          │    │
│  │  • Cron-based scheduling per source                              │    │
│  │  • Distributed clustering (multiple instances)                   │    │
│  │  • Persistent job store (PostgreSQL)                             │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                              │                                           │
│                              ▼                                           │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │                    SOURCE REGISTRY                               │    │
│  │  • Job board configurations                                      │    │
│  │  • Rate limits per source                                        │    │
│  │  • Authentication credentials (encrypted)                        │    │
│  │  • Extraction rules/selectors                                    │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                              │                                           │
│                              ▼                                           │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │                    CRAWL JOB DISPATCHER                          │    │
│  │  • Creates CrawlTask messages                                    │    │
│  │  • Distributes to worker queue                                   │    │
│  │  • Tracks job status                                             │    │
│  └─────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
                    [ MESSAGE QUEUE ]
                              │
          ┌───────────────────┼───────────────────┐
          ▼                   ▼                   ▼
┌───────────────────┐┌───────────────────┐┌───────────────────┐
│  CRAWLER WORKER 1 ││  CRAWLER WORKER 2 ││  CRAWLER WORKER N │
│  ─────────────────││  ─────────────────││  ─────────────────│
│  • HTTP Client    ││  • HTTP Client    ││  • HTTP Client    │
│  • Site Extractor ││  • Site Extractor ││  • Site Extractor │
│  • Rate Limiter   ││  • Rate Limiter   ││  • Rate Limiter   │
└───────────────────┘└───────────────────┘└───────────────────┘
```

### 6.2 Source Configuration Model

```json
{
  "id": "linkedin-dotnet-jobs",
  "name": "LinkedIn .NET Jobs",
  "enabled": true,
  "type": "api|html|rss",

  "schedule": {
    "cron": "0 */4 * * *",
    "timezone": "UTC"
  },

  "connection": {
    "base_url": "https://www.linkedin.com",
    "search_endpoint": "/jobs/search",
    "rate_limit": {
      "requests_per_minute": 10,
      "burst": 3
    },
    "headers": {
      "User-Agent": "JobRecon Crawler/1.0"
    }
  },

  "extraction": {
    "type": "css|xpath|json",
    "list_selector": ".job-card-container",
    "pagination": {
      "type": "offset|cursor|page",
      "param": "start",
      "increment": 25,
      "max_pages": 10
    },
    "fields": {
      "title": ".job-card-title",
      "company": ".job-card-company-name",
      "location": ".job-card-location",
      "url": "a.job-card-container__link@href"
    }
  },

  "search_parameters": {
    "keywords": [".NET", "C#", "Azure"],
    "location": "United States",
    "posted_within": "24h"
  }
}
```

### 6.3 Extractor Strategy Pattern

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     EXTRACTOR ARCHITECTURE                               │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  IJobExtractor                                                          │
│  ├── LinkedInExtractor                                                  │
│  ├── IndeedExtractor                                                    │
│  ├── GlassdoorExtractor                                                 │
│  ├── RemoteOkExtractor                                                  │
│  ├── StackOverflowExtractor                                             │
│  ├── GitHubJobsExtractor                                                │
│  └── GenericRSSExtractor                                                │
│                                                                          │
│  Each extractor implements:                                             │
│  • ExtractJobListAsync(html) → List<RawJob>                             │
│  • ExtractJobDetailAsync(html) → RawJobDetail                           │
│  • NeedsDetailFetch() → bool                                            │
│  • GetSourceIdentifier() → string                                       │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### 6.4 Resilience Patterns

| Pattern | Implementation | Purpose |
|---------|---------------|---------|
| **Retry** | Polly retry with exponential backoff | Handle transient failures |
| **Circuit Breaker** | Polly circuit breaker per source | Prevent cascade failures |
| **Rate Limiting** | Token bucket per source | Respect site limits |
| **Timeout** | 30s per request | Prevent hung connections |
| **Bulkhead** | Semaphore per worker | Limit concurrent requests |
| **Fallback** | Skip to next source on failure | Ensure partial success |

### 6.5 Deduplication Strategy

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     DEDUPLICATION PIPELINE                               │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  LEVEL 1: Exact Source Match                                            │
│  ───────────────────────────                                            │
│  • Check: source + source_id combination                                │
│  • Action: Update existing record, skip reprocessing                    │
│                                                                          │
│  LEVEL 2: Content Hash                                                  │
│  ─────────────────────                                                  │
│  • Generate: SHA256(title + company + description_first_500_chars)      │
│  • Check: Hash exists in jobs.normalized_listings                       │
│  • Action: Mark as duplicate, link to original                          │
│                                                                          │
│  LEVEL 3: Fuzzy Matching (Expensive - Run Periodically)                 │
│  ────────────────────────────────────────────────────                   │
│  • Compare: Title similarity (Levenshtein > 0.85)                       │
│  • Compare: Company name similarity                                      │
│  • Compare: Location match                                              │
│  • Action: Flag for review, potential merge                             │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 7. Recommended Infrastructure Layout

### 7.1 Kubernetes (k3s) Namespace Structure

```
jobrecon-system/
├── infrastructure/
│   ├── postgresql
│   ├── mongodb
│   ├── redis
│   ├── rabbitmq
│   ├── qdrant
│   └── minio
│
├── ai/
│   ├── ollama
│   └── ai-pipeline
│
├── services/
│   ├── identity-service
│   ├── profile-service
│   ├── job-processor
│   ├── job-query
│   ├── match-evaluator
│   └── notification-service
│
├── crawlers/
│   ├── crawler-orchestrator
│   └── crawler-workers (HPA: 1-5)
│
├── ingress/
│   ├── api-gateway
│   ├── traefik (k3s default)
│   └── cert-manager
│
├── frontend/
│   └── react-spa (nginx container)
│
└── observability/
    ├── prometheus
    ├── grafana
    ├── loki
    ├── jaeger
    └── otel-collector
```

### 7.2 k3s-Specific Configuration

```yaml
# k3s configuration considerations
k3s:
  # Traefik is included by default in k3s
  ingress_controller: traefik

  # Local path provisioner included
  storage_class: local-path

  # ServiceLB (Klipper) for LoadBalancer services
  load_balancer: servicelb

  # Flannel CNI by default
  cni: flannel

  # Disable unnecessary components for home lab
  disable:
    - traefik  # Optional: disable if using nginx-ingress
    - metrics-server  # Use prometheus instead

# Install command example:
# curl -sfL https://get.k3s.io | sh -s - --disable traefik
```

### 7.3 Resource Allocation (Local k3s Cluster)

| Component | CPU Request | CPU Limit | Memory Request | Memory Limit | Replicas |
|-----------|-------------|-----------|----------------|--------------|----------|
| PostgreSQL | 500m | 2000m | 1Gi | 4Gi | 1 |
| MongoDB | 500m | 2000m | 1Gi | 4Gi | 1 |
| Redis | 100m | 500m | 256Mi | 512Mi | 1 |
| RabbitMQ | 250m | 1000m | 512Mi | 1Gi | 1 |
| Qdrant | 500m | 2000m | 1Gi | 4Gi | 1 |
| Ollama | 1000m | 4000m | 4Gi | 12Gi | 1 |
| API Gateway | 100m | 500m | 256Mi | 512Mi | 2 |
| Identity Service | 100m | 500m | 256Mi | 512Mi | 2 |
| Profile Service | 100m | 500m | 256Mi | 512Mi | 2 |
| AI Pipeline | 250m | 1000m | 512Mi | 1Gi | 2 |
| Job Processor | 200m | 500m | 256Mi | 512Mi | 2 |
| Match Evaluator | 200m | 500m | 256Mi | 512Mi | 2 |
| Crawler Orchestrator | 100m | 250m | 128Mi | 256Mi | 1 |
| Crawler Workers | 100m | 250m | 128Mi | 256Mi | 1-5 (HPA) |
| Notification Service | 50m | 250m | 128Mi | 256Mi | 1 |
| React Frontend (nginx) | 50m | 100m | 64Mi | 128Mi | 2 |

**Total Estimated:** ~6 CPU cores, ~32GB RAM minimum

### 7.4 Storage Classes

```yaml
# k3s local-path provisioner (default)
apiVersion: storage.k8s.io/v1
kind: StorageClass
metadata:
  name: local-path
provisioner: rancher.io/local-path
volumeBindingMode: WaitForFirstConsumer
reclaimPolicy: Delete

---
# For databases requiring data persistence across node rebuilds
apiVersion: storage.k8s.io/v1
kind: StorageClass
metadata:
  name: local-persistent
provisioner: rancher.io/local-path
volumeBindingMode: WaitForFirstConsumer
reclaimPolicy: Retain
```

### 7.5 Network Policies

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     NETWORK SEGMENTATION                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  INGRESS ZONE (Public)                                                  │
│  • api-gateway ← external traffic                                       │
│  • react-frontend ← external traffic (static files)                    │
│                                                                          │
│  APPLICATION ZONE (Internal Only)                                       │
│  • All services communicate via ClusterIP                               │
│  • Services can reach Message Bus                                       │
│  • Services can reach Data Layer                                        │
│                                                                          │
│  DATA ZONE (Restricted)                                                 │
│  • PostgreSQL ← Application Zone only                                   │
│  • MongoDB ← Application Zone only                                      │
│  • Qdrant ← AI Zone + Application Zone                                  │
│  • Redis ← Application Zone only                                        │
│                                                                          │
│  AI ZONE (GPU Access)                                                   │
│  • Ollama ← AI Pipeline only                                            │
│  • AI Pipeline ← Message Bus only                                       │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 8. Local vs Azure Deployment Strategy

### 8.1 Component Placement Matrix

| Component | Local (k3s) | Azure | Reasoning |
|-----------|-------------|-------|-----------|
| **PostgreSQL** | ✓ Primary | Azure SQL (DR) | Cost; local performance adequate |
| **MongoDB** | ✓ Primary | CosmosDB (DR) | Cost; AI workload heavy |
| **Redis** | ✓ Primary | Azure Cache (optional) | Sub-ms latency local |
| **RabbitMQ** | ✓ Primary | — | Event volume high, cost |
| **—** | — | Service Bus | For cloud-only mode |
| **Qdrant** | ✓ Primary | — | Vector ops need low latency |
| **MinIO** | ✓ Primary | — | CV storage, local control |
| **—** | — | Azure Blob | For cloud-only mode |
| **Ollama** | ✓ Only | — | GPU cost prohibitive in cloud |
| **AI Pipeline** | ✓ Primary | — | Must be near Ollama |
| **Services** | ✓ Primary | AKS (optional) | Can run anywhere |
| **API Gateway** | ✓ Primary | Azure APIM (optional) | Local adequate |
| **React Frontend** | ✓ Primary | Static Web Apps | CDN for production |
| **Identity** | ✓ Primary | EntraID External Identities | Hybrid approach |

### 8.2 Hybrid Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        HYBRID DEPLOYMENT                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │                    AZURE (Public Access)                         │    │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │    │
│  │  │  Azure CDN  │  │  Azure DNS  │  │  Azure WAF  │              │    │
│  │  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘              │    │
│  │         └────────────────┼────────────────┘                      │    │
│  │                          │                                       │    │
│  │         ┌────────────────┼────────────────┐                      │    │
│  │         │      Azure Front Door           │                      │    │
│  │         └────────────────┬────────────────┘                      │    │
│  │                          │                                       │    │
│  │  ┌─────────────────────────────────────────────────────────┐     │    │
│  │  │              EntraID External Identities                │     │    │
│  │  │  • Google, GitHub, Microsoft account federation         │     │    │
│  │  │  • Custom policies for sign-up/sign-in                  │     │    │
│  │  └─────────────────────────────────────────────────────────┘     │    │
│  └──────────────────────────┼───────────────────────────────────────┘    │
│                             │                                            │
│                     [ CLOUDFLARE TUNNEL ]                               │
│                     [ OR VPN GATEWAY ]                                  │
│                             │                                            │
│  ┌──────────────────────────┼───────────────────────────────────────┐    │
│  │                    LOCAL k3s CLUSTER                              │    │
│  │                          │                                        │    │
│  │         ┌────────────────┼────────────────┐                       │    │
│  │         │          TRAEFIK INGRESS        │                       │    │
│  │         └────────────────┬────────────────┘                       │    │
│  │                          │                                        │    │
│  │  ┌─────────┬─────────┬───┴───┬─────────┬─────────┐               │    │
│  │  │ Gateway │ Services│ AI    │ Crawlers│  Data   │               │    │
│  │  │         │         │Pipeline│        │  Layer  │               │    │
│  │  └─────────┴─────────┴───────┴─────────┴─────────┘               │    │
│  └──────────────────────────────────────────────────────────────────┘    │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### 8.3 Identity Provider Configuration

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     IDENTITY PROVIDER STRATEGY                           │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  LOCAL & SELF-HOSTED:                                                   │
│  ─────────────────────                                                  │
│  • ASP.NET Core Identity (native user management)                       │
│  • PostgreSQL for user store                                            │
│  • Manual JWT Bearer token issuance                                     │
│  • Refresh token rotation                                               │
│                                                                          │
│  PRODUCTION (AZURE - FUTURE):                                           │
│  ─────────────────────────────                                          │
│  • EntraID External Identities (optional integration)                   │
│  • Federated providers: Google, GitHub, Microsoft                       │
│  • Custom sign-up/sign-in flows                                         │
│  • MFA support                                                          │
│                                                                          │
│  JWT CONFIGURATION:                                                     │
│  ──────────────────                                                     │
│  {                                                                       │
│    "Jwt": {                                                              │
│      "Issuer": "https://jobrecon.local",                                │
│      "Audience": "jobrecon-api",                                        │
│      "ExpiryMinutes": 60,                                               │
│      "RefreshExpiryDays": 7,                                            │
│      "SigningKey": ""  // From secrets, NEVER in config                 │
│    },                                                                    │
│    "ExternalProviders": {                                                │
│      "Google": { "ClientId": "...", "ClientSecret": "..." },           │
│      "GitHub": { "ClientId": "...", "ClientSecret": "..." }            │
│    }                                                                     │
│  }                                                                       │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### 8.4 Configuration Abstraction

```
Environment Configuration Layers:

┌─────────────────────────────────────────────────────────────────────────┐
│  LAYER 1: Base Configuration (shared)                                   │
│  • appsettings.json                                                     │
│  • Default timeouts, retry policies                                     │
│  • Feature flags (defaults)                                             │
└─────────────────────────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────────────────┐
│  LAYER 2: Environment Configuration                                     │
│  • appsettings.{Environment}.json                                       │
│  • Local: localhost connection strings, local IdP                       │
│  • Azure: Azure service connection strings, EntraID                     │
└─────────────────────────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────────────────┐
│  LAYER 3: Secrets (Kubernetes Secrets / Azure Key Vault)                │
│  • Database passwords                                                   │
│  • API keys                                                             │
│  • SMTP credentials                                                     │
│  • OAuth client secrets                                                 │
└─────────────────────────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────────────────┐
│  LAYER 4: Runtime Configuration (ConfigMaps / App Configuration)        │
│  • Feature toggles                                                      │
│  • Rate limits                                                          │
│  • Crawler schedules                                                    │
└─────────────────────────────────────────────────────────────────────────┘
```

### 8.5 Service Abstraction Interfaces

```
Abstractions to enable deployment flexibility:

IMessageBus
├── RabbitMqMessageBus (Local)
└── AzureServiceBusMessageBus (Cloud)

IBlobStorage
├── MinioStorage (Local)
└── AzureBlobStorage (Cloud)

ISecretProvider
├── KubernetesSecretProvider (Local)
└── AzureKeyVaultProvider (Cloud)

IDistributedCache
├── RedisCache (Both)
└── AzureCacheForRedis (Cloud)

ITokenService
├── JwtTokenService (ASP.NET Core Identity + manual JWT)
└── EntraIdTokenService (Cloud - EntraID External Identities, future)
```

---

## 9. Security & Secrets Management

> ⚠️ **CRITICAL: No secrets shall ever be committed to the repository. This is non-negotiable.**

### 9.1 Secrets Management Principles

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     SECRETS MANAGEMENT RULES                             │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ❌ NEVER commit to repository:                                         │
│  ────────────────────────────                                           │
│  • Database connection strings with passwords                           │
│  • API keys (external services, LLM providers, etc.)                   │
│  • OAuth client secrets                                                 │
│  • JWT signing keys                                                     │
│  • SMTP credentials                                                     │
│  • Cloud provider credentials (Azure, AWS)                             │
│  • Private keys, certificates                                           │
│  • Any password or token                                                │
│                                                                          │
│  ✅ ALWAYS use:                                                         │
│  ──────────────                                                         │
│  • Environment variables                                                │
│  • Kubernetes Secrets (encrypted at rest)                               │
│  • Azure Key Vault (production)                                         │
│  • GitHub Secrets (CI/CD)                                               │
│  • .NET User Secrets (local development only)                          │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### 9.2 Secrets by Environment

| Environment | Secrets Store | Access Method |
|-------------|--------------|---------------|
| **Local Development** | .NET User Secrets | `dotnet user-secrets` |
| **Local k3s** | Kubernetes Secrets + Sealed Secrets | Environment variables |
| **GitHub Actions** | GitHub Secrets | `${{ secrets.* }}` |
| **Azure Production** | Azure Key Vault | Managed Identity |

### 9.3 Local Development Setup

```bash
# Initialize user secrets for each service (run once per project)
cd src/Services/JobRecon.Identity
dotnet user-secrets init

# Set secrets locally (NEVER commit these values)
dotnet user-secrets set "ConnectionStrings:PostgreSQL" "Host=localhost;Database=jobrecon;Username=dev;Password=YOUR_LOCAL_PASSWORD"
dotnet user-secrets set "Jwt:SigningKey" "your-256-bit-secret-key-here"
dotnet user-secrets set "OAuth:Google:ClientSecret" "your-google-client-secret"
dotnet user-secrets set "OAuth:GitHub:ClientSecret" "your-github-client-secret"
dotnet user-secrets set "Smtp:Password" "your-smtp-password"
```

### 9.4 Configuration Files (Safe to Commit)

```json
// appsettings.json - NO SECRETS, only structure and defaults
{
  "ConnectionStrings": {
    "PostgreSQL": ""  // Empty - populated from secrets
  },
  "Jwt": {
    "Issuer": "https://jobrecon.local",
    "Audience": "jobrecon-api",
    "ExpiryMinutes": 60,
    "SigningKey": ""  // Empty - populated from secrets
  },
  "OAuth": {
    "Google": {
      "ClientId": "",  // Can be in config for non-sensitive
      "ClientSecret": ""  // MUST be from secrets
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

```json
// appsettings.Development.json - Local URLs only, NO SECRETS
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Database=jobrecon_dev;Username=dev;Password="
    // Password portion comes from user-secrets
  },
  "Services": {
    "Ollama": "http://localhost:11434",
    "Qdrant": "http://localhost:6333",
    "RabbitMQ": "localhost"
  }
}
```

### 9.5 Kubernetes Secrets (Local k3s)

```yaml
# deploy/k8s/base/secrets.yaml
# This file is a TEMPLATE - actual values injected by Sealed Secrets or external-secrets
apiVersion: v1
kind: Secret
metadata:
  name: jobrecon-secrets
  namespace: jobrecon-services
type: Opaque
stringData:
  # All values are placeholders - replaced during deployment
  POSTGRES_PASSWORD: "${POSTGRES_PASSWORD}"
  JWT_SIGNING_KEY: "${JWT_SIGNING_KEY}"
  OAUTH_GOOGLE_SECRET: "${OAUTH_GOOGLE_SECRET}"
  OAUTH_GITHUB_SECRET: "${OAUTH_GITHUB_SECRET}"
  SMTP_PASSWORD: "${SMTP_PASSWORD}"
  RABBITMQ_PASSWORD: "${RABBITMQ_PASSWORD}"
```

```yaml
# Using Sealed Secrets (recommended for GitOps)
# Install: helm install sealed-secrets sealed-secrets/sealed-secrets

# Create sealed secret (encrypted, safe to commit)
# kubeseal --format yaml < secret.yaml > sealed-secret.yaml

apiVersion: bitnami.com/v1alpha1
kind: SealedSecret
metadata:
  name: jobrecon-secrets
  namespace: jobrecon-services
spec:
  encryptedData:
    POSTGRES_PASSWORD: AgBf8s9f... # Encrypted value
    JWT_SIGNING_KEY: AgC2kd8j...   # Encrypted value
```

### 9.6 GitHub Actions Secrets

```yaml
# Required GitHub Secrets (configure in repository settings):
#
# GHCR_TOKEN          - GitHub Container Registry access
# KUBECONFIG_LOCAL    - Base64-encoded kubeconfig for self-hosted runner
# POSTGRES_PASSWORD   - Database password
# JWT_SIGNING_KEY     - JWT signing key
# OAUTH_GOOGLE_SECRET - Google OAuth client secret
# OAUTH_GITHUB_SECRET - GitHub OAuth client secret
# SMTP_PASSWORD       - Email service password
# AZURE_CREDENTIALS   - Azure service principal (for Azure deployments)
# AZURE_KEYVAULT_URL  - Azure Key Vault URL

# Usage in workflows:
- name: Deploy with secrets
  env:
    POSTGRES_PASSWORD: ${{ secrets.POSTGRES_PASSWORD }}
  run: |
    helm upgrade --install jobrecon ./deploy/helm/jobrecon \
      --set secrets.postgresPassword="${POSTGRES_PASSWORD}"
```

### 9.7 Azure Key Vault Integration

```csharp
// Program.cs - Azure Key Vault configuration
var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault in production
if (builder.Environment.IsProduction())
{
    var keyVaultUrl = builder.Configuration["Azure:KeyVaultUrl"];
    if (!string.IsNullOrEmpty(keyVaultUrl))
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultUrl),
            new DefaultAzureCredential());
    }
}

// Secrets are now available via IConfiguration
// builder.Configuration["PostgreSQL:Password"]
// builder.Configuration["Jwt:SigningKey"]
```

### 9.8 Git Security Configuration

```gitignore
# .gitignore - CRITICAL security exclusions

# Secrets and credentials
*.pfx
*.p12
*.key
*.pem
secrets.json
secrets.yaml
secrets.yml
**/secrets/
.env
.env.*
!.env.example

# User secrets
**/usersecrets/

# Local settings with potential secrets
appsettings.Local.json
appsettings.*.Local.json
local.settings.json

# Kubernetes secrets (unencrypted)
*-secret.yaml
*-secrets.yaml
!*-sealed-secret.yaml

# IDE and tool configs that may contain paths/secrets
.idea/
*.suo
*.user
.vs/

# Terraform state (may contain secrets)
*.tfstate
*.tfstate.*

# Helm values with secrets
values-secrets.yaml
**/charts/**/secrets/
```

```yaml
# .github/workflows/security-scan.yml
name: Security Scan

on:
  push:
    branches: [main, develop]
  pull_request:

jobs:
  secrets-scan:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: TruffleHog Secret Scan
        uses: trufflesecurity/trufflehog@main
        with:
          path: ./
          base: ${{ github.event.repository.default_branch }}
          extra_args: --only-verified

      - name: Gitleaks Secret Scan
        uses: gitleaks/gitleaks-action@v2
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}

  dependency-scan:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Run Snyk to check for vulnerabilities
        uses: snyk/actions/dotnet@master
        env:
          SNYK_TOKEN: ${{ secrets.SNYK_TOKEN }}
        with:
          args: --severity-threshold=high
```

### 9.9 Pre-commit Hooks

```yaml
# .pre-commit-config.yaml
repos:
  - repo: https://github.com/gitleaks/gitleaks
    rev: v8.18.0
    hooks:
      - id: gitleaks

  - repo: https://github.com/Yelp/detect-secrets
    rev: v1.4.0
    hooks:
      - id: detect-secrets
        args: ['--baseline', '.secrets.baseline']

  - repo: local
    hooks:
      - id: check-secrets-in-config
        name: Check for secrets in config files
        entry: bash -c 'grep -r -l "password.*=.*[^$\"{]" --include="*.json" --include="*.yaml" --include="*.yml" . && exit 1 || exit 0'
        language: system
        pass_filenames: false
```

```bash
# Install pre-commit hooks
pip install pre-commit
pre-commit install

# Run manually
pre-commit run --all-files
```

### 9.10 Secrets Rotation Policy

| Secret Type | Rotation Frequency | Method |
|-------------|-------------------|--------|
| Database passwords | 90 days | Kubernetes Job + Secret update |
| JWT signing keys | 180 days | Key rotation with overlap period |
| OAuth client secrets | 365 days | Provider console + update secrets |
| API keys | 90 days | Regenerate + deploy |
| SMTP credentials | 180 days | Provider console + update secrets |

### 9.11 Emergency Secret Rotation

```bash
#!/bin/bash
# tools/scripts/rotate-secrets.sh

# Emergency secret rotation script
# Run when a secret may have been compromised

echo "⚠️  EMERGENCY SECRET ROTATION"
echo "This will rotate all secrets in the specified environment."

read -p "Environment (staging/production): " ENV
read -p "Are you sure? (yes/no): " CONFIRM

if [ "$CONFIRM" != "yes" ]; then
    echo "Aborted."
    exit 1
fi

# Generate new secrets
NEW_JWT_KEY=$(openssl rand -base64 32)
NEW_DB_PASSWORD=$(openssl rand -base64 24)

# Update Kubernetes secrets
kubectl create secret generic jobrecon-secrets \
    --namespace jobrecon-$ENV \
    --from-literal=JWT_SIGNING_KEY=$NEW_JWT_KEY \
    --from-literal=POSTGRES_PASSWORD=$NEW_DB_PASSWORD \
    --dry-run=client -o yaml | kubectl apply -f -

# Restart deployments to pick up new secrets
kubectl rollout restart deployment -n jobrecon-$ENV

# Update database password
kubectl exec -n jobrecon-$ENV deploy/postgresql -- \
    psql -c "ALTER USER jobrecon PASSWORD '$NEW_DB_PASSWORD';"

echo "✅ Secrets rotated. Update GitHub Secrets manually if needed."
echo "⚠️  Remember to update any external integrations."
```

---

## 10. Technology Choices and Justification

### 10.1 Backend Framework

| Choice | Technology | Justification |
|--------|------------|---------------|
| **Web Framework** | ASP.NET Core 10 Minimal APIs | Lightweight, high performance, modern syntax, LTS |
| **OpenAPI** | NSwag.AspNetCore | .NET-native, generates specs + TypeScript clients |
| **API Documentation** | Scalar UI | Modern alternative to SwaggerUI |
| **Background Jobs** | .NET Worker Services | Native integration, IHostedService support |
| **Scheduler** | Quartz.NET | Mature, distributed clustering, cron support |
| **HTTP Client** | IHttpClientFactory + Polly | Connection pooling, resilience patterns |
| **Serialization** | System.Text.Json | Native, fast, AOT-friendly |
| **Validation** | FluentValidation | Expressive, testable validation rules |
| **Mapping** | Mapperly | Source-generated, zero reflection |

### 10.2 Communication

| Choice | Technology | Justification |
|--------|------------|---------------|
| **External API** | REST (Minimal APIs) + NSwag | Standard, tooling, .NET-native client generation |
| **Internal Sync** | gRPC | Strongly typed, efficient, code-gen |
| **Async Messaging** | MassTransit + RabbitMQ | Powerful abstractions, sagas, retries, outbox pattern |
| **Real-time (future)** | SignalR | .NET native WebSocket support |

### 10.3 Data Storage

| Choice | Technology | Justification |
|--------|------------|---------------|
| **Relational** | PostgreSQL 16 | Open source, JSON support, mature |
| **Document** | MongoDB 7 | Flexible schema for job documents |
| **Vector** | Qdrant | Purpose-built, Rust performance, filtering |
| **Cache** | Redis 7 | Industry standard, pub/sub, streams |
| **Blob** | MinIO | S3-compatible, self-hosted |
| **ORM** | EF Core 10 | Productivity, migrations, LINQ |
| **MongoDB Driver** | Official C# Driver | First-party support |

### 10.4 AI/ML

| Choice | Technology | Justification |
|--------|------------|---------------|
| **Model Runtime** | Ollama | Easy model management, REST API |
| **Embedding Model** | nomic-embed-text | Good quality, reasonable size |
| **LLM** | Mistral 7B / Llama 3.2 | Local inference, no API costs |
| **Vector Search** | Qdrant | Native filtering, fast cosine |
| **.NET Integration** | OllamaSharp | Typed client for Ollama API |

### 10.5 Frontend

| Choice | Technology | Justification |
|--------|------------|---------------|
| **Framework** | React 18 + TypeScript | Industry standard, large ecosystem, type safety |
| **Build Tool** | Vite | Fast HMR, modern ESM-based bundling |
| **UI Library** | MUI (Material UI) | Comprehensive component library, Material Design, accessible |
| **State Management** | TanStack Query + Zustand | Server state + client state separation |
| **Routing** | React Router v6 | Standard routing solution |
| **Forms** | React Hook Form + Zod | Performant forms, schema validation |
| **API Client** | NSwag (generated) | .NET-native, type-safe TypeScript clients |
| **HTTP Client** | Fetch API | Native browser API, no dependencies |
| **Testing** | Vitest + Testing Library | Fast unit tests, RTL for components |
| **E2E Testing** | Playwright | Cross-browser, reliable |

### 10.6 OpenAPI Client Generation (NSwag)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     NSWAG CLIENT GENERATION PIPELINE                     │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  BACKEND (Build Time):                                                  │
│  ─────────────────────                                                  │
│  1. ASP.NET services generate OpenAPI specs via NSwag.AspNetCore        │
│  2. Specs exported to /api-specs/*.json during build                   │
│  3. API Gateway aggregates specs into unified spec                     │
│                                                                          │
│  FRONTEND (Build Time):                                                 │
│  ──────────────────────                                                 │
│  1. NSwag CLI generates TypeScript clients with Fetch API              │
│  2. Generated clients placed in /frontend/src/api/generated/           │
│  3. TanStack Query wrapper hooks for data fetching                     │
│                                                                          │
│  NSwag Advantages:                                                       │
│  • .NET-native tooling, same ecosystem                                  │
│  • Generates both OpenAPI spec and TypeScript client                    │
│  • Built-in support for authentication headers                          │
│  • Strongly typed request/response models                               │
│                                                                          │
│  GitHub Actions Integration:                                             │
│  • Backend spec generation in .NET build step                           │
│  • Frontend client regeneration triggered by spec changes               │
│  • Breaking change detection via openapi-diff                           │
│  • Automated PR checks for API compatibility                            │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### 10.7 Infrastructure & DevOps

| Choice | Technology | Justification |
|--------|------------|---------------|
| **Version Control** | GitHub | Industry standard, excellent CI/CD integration |
| **CI/CD** | GitHub Actions | Native GitHub integration, generous free tier |
| **Container Registry** | GitHub Container Registry (ghcr.io) | Integrated with GitHub, free for public repos |
| **Container Runtime** | containerd | K8s standard |
| **Orchestration** | k3s | Lightweight K8s, ideal for home lab |
| **Package Management** | Helm 3 | Templating, release management |
| **Ingress** | Traefik | k3s default, automatic HTTPS, dashboard included |
| **Certificates** | cert-manager | Automated Let's Encrypt |
| **GitOps** | ArgoCD | Great UI, GitHub integration, declarative deployments |
| **DB Migrations** | EF Core Migrations | Run as separate CI job before deployment |
| **Repository** | Monorepo | Single repo for all services + frontend |

### 10.8 Observability

| Choice | Technology | Justification |
|--------|------------|---------------|
| **Metrics** | Prometheus + Grafana | Standard stack |
| **Logging** | Loki + Promtail | Log aggregation, Grafana native |
| **Tracing** | Jaeger + OpenTelemetry | Distributed tracing |
| **Instrumentation** | OpenTelemetry .NET | Vendor-neutral |
| **Health Checks** | ASP.NET Health Checks | Native, Kubernetes probes |

### 10.9 Identity & Authentication

| Choice | Technology | Justification |
|--------|------------|---------------|
| **User Management** | ASP.NET Core Identity | Native .NET, built-in user/role management, password hashing |
| **Token Issuance** | Manual JWT Bearer | Simple, no OIDC server overhead, full control |
| **Cloud IdP** | EntraID External Identities | Enterprise-grade, social login federation (future) |
| **Token Format** | JWT (RS256) | Standard, stateless validation |
| **Session** | Refresh token rotation | Security best practice |
| **Frontend Auth** | Custom auth context + fetch interceptors | Lightweight, works with NSwag clients |

---

## 11. Suggested Development Phases

### Phase 1: Foundation

**Goal:** Core infrastructure, authentication, and frontend scaffolding

| Deliverable | Description |
|-------------|-------------|
| GitHub repository setup | Monorepo with projects structure, branch protection |
| **Secrets management setup** | **Pre-commit hooks, .gitignore, user-secrets, Sealed Secrets** |
| **Security scanning** | **TruffleHog, Gitleaks in CI pipeline** |
| Solution architecture | Shared libraries, contracts, common |
| Docker Compose | Local development environment |
| k3s setup scripts | Local cluster provisioning |
| Identity Service | Registration, login, JWT (local IdP) |
| API Gateway | Basic routing, auth middleware, OpenAPI aggregation |
| React app scaffold | Vite + TypeScript + routing + auth flow |
| OpenAPI codegen pipeline | Automated client generation |
| GitHub Actions CI | Build, test, lint, security scan, container images to ghcr.io |
| GitHub Actions CD | Deploy to local k3s via self-hosted runner |

**Key Decisions:**
- **Establish secrets management policy (NO secrets in code)**
- Establish coding standards and PR review process
- Define shared contracts/events
- Set up GitHub Actions workflow structure
- Choose OpenAPI codegen tool
- Configure branch protection rules
- **Set up pre-commit hooks for secret detection**

---

### Phase 2: Profile Management

**Goal:** User profiles and CV handling

| Deliverable | Description |
|-------------|-------------|
| Profile Service | CRUD operations with OpenAPI spec |
| Skills management | Add/remove skills with taxonomy |
| CV upload | File storage to MinIO |
| CV parsing | Basic LLM extraction |
| Profile UI | React pages for profile management |
| PostgreSQL schema | Profile domain tables |
| Generated API client | Profile service TypeScript client |

**Key Decisions:**
- Skill taxonomy structure
- CV parsing prompt design
- Storage retention policy

---

### Phase 3: Job Crawler Infrastructure

**Goal:** Automated job discovery

| Deliverable | Description |
|-------------|-------------|
| Crawler Orchestrator | Quartz.NET scheduler |
| First extractor | LinkedIn or Indeed |
| Job Processor | Normalization pipeline |
| MongoDB setup | Job document storage |
| RabbitMQ setup | Message bus |
| Deduplication | Content hash-based |

**Key Decisions:**
- Crawler politeness policies
- Job schema standardization
- Message retry strategies

---

### Phase 4: AI Pipeline & Vector Search

**Goal:** Semantic matching capability

| Deliverable | Description |
|-------------|-------------|
| Ollama deployment | K8s pod with models |
| AI Pipeline Service | Embedding generation |
| Qdrant setup | Collections and indexing |
| Profile embeddings | Auto-generate on change |
| Job embeddings | Batch generation |
| Vector search | Basic similarity queries |

**Key Decisions:**
- Embedding model selection
- Vector dimension and distance metric
- Batch processing strategy

---

### Phase 5: Match Evaluation

**Goal:** Intelligent job matching

| Deliverable | Description |
|-------------|-------------|
| Match Evaluator | Full pipeline |
| Metadata scoring | Skill/location adjustments |
| LLM evaluation | Deep match analysis |
| Match storage | PostgreSQL results |
| Job Query Service | User match retrieval with OpenAPI |
| Matches UI | React pages for viewing matches |
| Generated clients | Match/Query TypeScript clients |

**Key Decisions:**
- Scoring weights
- LLM evaluation prompts
- Threshold defaults

---

### Phase 6: Notifications

**Goal:** User engagement

| Deliverable | Description |
|-------------|-------------|
| Notification Service | Email dispatch |
| Email templates | Match notification design |
| Preference management | Threshold, frequency settings |
| Digest mode | Batch notifications |
| Notification history | Track sent notifications |
| Notification UI | React preferences page |

**Key Decisions:**
- SMTP provider
- Template engine
- Digest scheduling

---

### Phase 7: External Identity Providers

**Goal:** EntraID and social login integration

| Deliverable | Description |
|-------------|-------------|
| EntraID setup | External Identities configuration |
| Google OAuth | Social login integration |
| GitHub OAuth | Developer login option |
| Identity abstraction | Provider-agnostic auth layer |
| Frontend OIDC | MSAL.js or oidc-client integration |
| Account linking | Link external to existing accounts |

**Key Decisions:**
- User provisioning strategy
- Claim mapping
- Account merge policy

---

### Phase 8: Production Hardening

**Goal:** Production readiness

| Deliverable | Description |
|-------------|-------------|
| Helm charts | All services packaged |
| Observability stack | Prometheus, Grafana, Loki |
| Distributed tracing | OpenTelemetry + Jaeger |
| Health checks | K8s probes |
| Rate limiting | API gateway config |
| Security audit | OWASP review |
| Load testing | K6 or similar |

**Key Decisions:**
- Alerting thresholds
- SLO definitions
- Backup strategies

---

### Phase 9: Scale & Polish

**Goal:** Feature completeness

| Deliverable | Description |
|-------------|-------------|
| Additional crawlers | 3-5 job sources |
| Advanced filters | Enhanced job query UI |
| Application tracking | Track apply status |
| Analytics dashboard | User engagement metrics |
| Admin portal | System management (React) |
| Documentation | API docs, runbooks |

---

### Suggested Timeline Structure

```
Phase 1: Foundation              ████████
Phase 2: Profile Management          ██████
Phase 3: Job Crawlers                    ████████
Phase 4: AI Pipeline                         ██████████
Phase 5: Match Evaluation                          ████████
Phase 6: Notifications                                   ████
Phase 7: External Identity                                   ██████
Phase 8: Production Hardening                                      ██████████
Phase 9: Scale & Polish                                                    ████████

Sequential phases with overlap where dependencies allow.
```

---

## Appendix A: Project Structure

```
JobRecon/
├── .github/
│   ├── workflows/
│   │   ├── ci.yml                     # Main CI pipeline
│   │   ├── cd-local.yml               # Deploy to local k3s
│   │   ├── cd-azure.yml               # Deploy to Azure
│   │   ├── openapi-check.yml          # API breaking change detection
│   │   ├── security-scan.yml          # Secret scanning & dependency audit
│   │   └── release.yml                # Release automation
│   ├── CODEOWNERS
│   └── dependabot.yml
│
├── .gitignore                         # Security-focused exclusions
├── .pre-commit-config.yaml            # Pre-commit hooks for secret detection
├── .secrets.baseline                  # detect-secrets baseline
│
├── src/
│   ├── Shared/
│   │   ├── JobRecon.Contracts/          # Event contracts, DTOs, OpenAPI models
│   │   ├── JobRecon.Domain/             # Domain entities, value objects
│   │   └── JobRecon.Infrastructure/     # Cross-cutting (logging, auth, identity)
│   │
│   ├── Services/
│   │   ├── JobRecon.Identity/           # Identity Service
│   │   ├── JobRecon.Profile/            # Profile Service
│   │   ├── JobRecon.Jobs.Crawler/       # Crawler Workers
│   │   ├── JobRecon.Jobs.Orchestrator/  # Crawler Orchestrator
│   │   ├── JobRecon.Jobs.Processor/     # Job Processor
│   │   ├── JobRecon.Jobs.Query/         # Job Query Service
│   │   ├── JobRecon.AI.Pipeline/        # AI Pipeline Service
│   │   ├── JobRecon.Matching/           # Match Evaluator
│   │   └── JobRecon.Notifications/      # Notification Service
│   │
│   └── Gateway/
│       └── JobRecon.Gateway/            # API Gateway (YARP + OpenAPI aggregation)
│
├── frontend/
│   ├── src/
│   │   ├── api/
│   │   │   ├── generated/               # NSwag-generated TypeScript clients
│   │   │   └── hooks/                   # TanStack Query wrappers
│   │   ├── components/
│   │   │   ├── common/                  # Shared MUI components
│   │   │   └── features/                # Feature-specific components
│   │   ├── pages/                       # Route pages
│   │   ├── stores/                      # Zustand stores
│   │   ├── theme/                       # MUI theme configuration
│   │   ├── lib/                         # Utilities
│   │   └── App.tsx
│   ├── package.json
│   ├── vite.config.ts
│   ├── tsconfig.json
│   └── nswag.json                       # NSwag client generation config
│
├── api-specs/                           # Generated OpenAPI specs
│   ├── identity.openapi.json
│   ├── profile.openapi.json
│   ├── jobs.openapi.json
│   ├── matching.openapi.json
│   └── aggregated.openapi.json
│
├── tests/
│   ├── Unit/
│   ├── Integration/
│   └── E2E/
│
├── deploy/
│   ├── docker/
│   │   └── docker-compose.yml
│   ├── k3s/
│   │   ├── setup/                       # k3s installation scripts
│   │   └── manifests/
│   ├── k8s/
│   │   ├── base/
│   │   └── overlays/
│   │       ├── local/
│   │       └── azure/
│   └── helm/
│       └── jobrecon/
│
├── docs/
│   ├── architecture/
│   ├── api/
│   └── runbooks/
│
├── tools/
│   └── scripts/
│
├── JobRecon.sln
└── README.md
```

---

## Appendix B: NSwag Integration Details

### B.1 Backend NSwag Configuration

```csharp
// Program.cs (each service)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "JobRecon Profile Service";
    config.Version = "v1";
    config.Description = "Profile management API";

    // JWT Bearer authentication
    config.AddSecurity("Bearer", new OpenApiSecurityScheme
    {
        Type = OpenApiSecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter your JWT token"
    });
});

// Serve OpenAPI spec and Swagger UI
app.UseOpenApi();       // /swagger/v1/swagger.json
app.UseSwaggerUi();     // /swagger
app.UseReDoc();         // /redoc (alternative docs)
```

### B.2 NSwag TypeScript Client Generation

```json
// nswag.json - NSwag configuration for TypeScript generation
{
  "runtime": "Net80",
  "documentGenerator": {
    "fromDocument": {
      "url": "http://localhost:5000/swagger/v1/swagger.json"
    }
  },
  "codeGenerators": {
    "openApiToTypeScriptClient": {
      "className": "{controller}Client",
      "moduleName": "",
      "namespace": "",
      "typeScriptVersion": 5.0,
      "template": "Fetch",
      "promiseType": "Promise",
      "httpClass": "HttpClient",
      "dateTimeType": "Date",
      "nullValue": "Undefined",
      "generateClientClasses": true,
      "generateClientInterfaces": true,
      "generateOptionalParameters": true,
      "generateResponseClasses": true,
      "wrapResponses": false,
      "generateDtoTypes": true,
      "output": "../frontend/src/api/generated/api-client.ts"
    }
  }
}
```

```bash
# Generate TypeScript client (run from solution root)
# Add to package.json scripts or run directly
nswag run nswag.json
```

### B.3 Generated Client Usage with TanStack Query

```typescript
// frontend/src/api/hooks/useProfile.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ProfileClient, ProfileUpdateDto } from '../generated/api-client';

const profileClient = new ProfileClient();

export function useGetUserProfile() {
  return useQuery({
    queryKey: ['profile'],
    queryFn: () => profileClient.getProfile(),
  });
}

export function useUpdateUserProfile() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: ProfileUpdateDto) => profileClient.updateProfile(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['profile'] });
    },
  });
}
```

```typescript
// frontend/src/pages/ProfilePage.tsx
import { useGetUserProfile, useUpdateUserProfile } from '@/api/hooks/useProfile';
import { CircularProgress } from '@mui/material';

function ProfilePage() {
  const { data: profile, isLoading } = useGetUserProfile();
  const updateMutation = useUpdateUserProfile();

  const handleSubmit = (data: ProfileUpdateDto) => {
    updateMutation.mutate(data);
  };

  if (isLoading) return <CircularProgress />;

  return <ProfileForm profile={profile} onSubmit={handleSubmit} />;
}
```

### B.4 CI Integration for Client Generation

```yaml
# In .github/workflows/ci.yml
- name: Generate OpenAPI Specs
  run: |
    dotnet build --configuration Release
    # Export specs from running services or use NSwag CLI
    dotnet tool restore
    nswag run nswag.json /runtime:Net80

- name: Build Frontend with Generated Clients
  working-directory: frontend
  run: |
    npm ci
    npm run build
```

---

## Appendix C: k3s Setup Script

```bash
#!/bin/bash
# deploy/k3s/setup/install.sh

# Install k3s (single node, disable traefik to use nginx-ingress optionally)
curl -sfL https://get.k3s.io | sh -s - \
  --write-kubeconfig-mode 644 \
  --disable traefik

# Wait for k3s to be ready
kubectl wait --for=condition=Ready nodes --all --timeout=60s

# Install Helm
curl https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash

# Add Helm repos
helm repo add bitnami https://charts.bitnami.com/bitnami
helm repo add jetstack https://charts.jetstack.io
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo update

# Install NGINX Ingress Controller
helm install ingress-nginx ingress-nginx/ingress-nginx \
  --namespace ingress-nginx --create-namespace

# Install cert-manager
helm install cert-manager jetstack/cert-manager \
  --namespace cert-manager --create-namespace \
  --set installCRDs=true

# Create namespaces
kubectl create namespace jobrecon-infra
kubectl create namespace jobrecon-services
kubectl create namespace jobrecon-ai
kubectl create namespace jobrecon-observability

echo "k3s cluster ready for JobRecon deployment"
```

---

## Appendix D: GitHub Actions CI/CD Pipelines

### D.1 CI Pipeline Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     GITHUB ACTIONS CI/CD ARCHITECTURE                    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  TRIGGERS:                                                               │
│  ─────────                                                              │
│  • Push to main/develop branches                                        │
│  • Pull request to main/develop                                         │
│  • Manual workflow dispatch                                             │
│  • Scheduled (nightly builds)                                           │
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │                    CI PIPELINE (ci.yml)                          │    │
│  ├─────────────────────────────────────────────────────────────────┤    │
│  │                                                                  │    │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐        │    │
│  │  │  Lint &  │  │  Build   │  │   Test   │  │  Build   │        │    │
│  │  │  Format  │  │  .NET    │  │  (Unit)  │  │  Docker  │        │    │
│  │  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘        │    │
│  │       │             │             │             │               │    │
│  │       └─────────────┴──────┬──────┴─────────────┘               │    │
│  │                            │                                     │    │
│  │                            ▼                                     │    │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐        │    │
│  │  │ OpenAPI  │  │ Frontend │  │Integration│  │  Push to │        │    │
│  │  │   Gen    │  │  Build   │  │  Tests   │  │ ghcr.io  │        │    │
│  │  └──────────┘  └──────────┘  └──────────┘  └──────────┘        │    │
│  │                                                                  │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │                    CD PIPELINE (cd-local.yml)                    │    │
│  ├─────────────────────────────────────────────────────────────────┤    │
│  │  Runs on: self-hosted runner in home lab                        │    │
│  │                                                                  │    │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐        │    │
│  │  │  Pull    │  │  Helm    │  │  Deploy  │  │  Health  │        │    │
│  │  │  Images  │  │ Template │  │  to k3s  │  │  Check   │        │    │
│  │  └──────────┘  └──────────┘  └──────────┘  └──────────┘        │    │
│  │                                                                  │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### D.2 Main CI Workflow

```yaml
# .github/workflows/ci.yml
name: CI Pipeline

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

env:
  DOTNET_VERSION: '10.0.x'
  NODE_VERSION: '20.x'
  REGISTRY: ghcr.io
  IMAGE_PREFIX: ${{ github.repository_owner }}/jobrecon

jobs:
  # ====================
  # Backend Jobs
  # ====================
  lint-backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      - name: Restore
        run: dotnet restore
      - name: Format Check
        run: dotnet format --verify-no-changes

  build-backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      - name: Restore
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore --configuration Release
      - name: Generate OpenAPI Specs
        run: |
          dotnet tool restore
          dotnet swagger tofile --output api-specs/identity.json src/Services/JobRecon.Identity/bin/Release/net10.0/JobRecon.Identity.dll v1
          dotnet swagger tofile --output api-specs/profile.json src/Services/JobRecon.Profile/bin/Release/net10.0/JobRecon.Profile.dll v1
      - name: Upload OpenAPI Specs
        uses: actions/upload-artifact@v4
        with:
          name: openapi-specs
          path: api-specs/

  test-backend:
    runs-on: ubuntu-latest
    needs: build-backend
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      - name: Test
        run: dotnet test --configuration Release --collect:"XPlat Code Coverage" --results-directory ./coverage
      - name: Upload Coverage
        uses: codecov/codecov-action@v4
        with:
          directory: ./coverage

  # ====================
  # Frontend Jobs
  # ====================
  lint-frontend:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: frontend
    steps:
      - uses: actions/checkout@v4
      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}
          cache: 'npm'
          cache-dependency-path: frontend/package-lock.json
      - run: npm ci
      - run: npm run lint
      - run: npm run type-check

  build-frontend:
    runs-on: ubuntu-latest
    needs: [build-backend]
    defaults:
      run:
        working-directory: frontend
    steps:
      - uses: actions/checkout@v4
      - name: Download OpenAPI Specs
        uses: actions/download-artifact@v4
        with:
          name: openapi-specs
          path: api-specs/
      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}
          cache: 'npm'
          cache-dependency-path: frontend/package-lock.json
      - run: npm ci
      - name: Generate API Client
        run: npm run generate-api
      - run: npm run build
      - name: Upload Build
        uses: actions/upload-artifact@v4
        with:
          name: frontend-build
          path: frontend/dist/

  test-frontend:
    runs-on: ubuntu-latest
    needs: build-frontend
    defaults:
      run:
        working-directory: frontend
    steps:
      - uses: actions/checkout@v4
      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}
          cache: 'npm'
          cache-dependency-path: frontend/package-lock.json
      - run: npm ci
      - run: npm run test:coverage

  # ====================
  # Docker Build Jobs
  # ====================
  build-images:
    runs-on: ubuntu-latest
    needs: [test-backend, test-frontend]
    if: github.event_name == 'push'
    permissions:
      contents: read
      packages: write
    strategy:
      matrix:
        service:
          - identity
          - profile
          - jobs-crawler
          - jobs-processor
          - jobs-query
          - ai-pipeline
          - matching
          - notifications
          - gateway
    steps:
      - uses: actions/checkout@v4
      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v3
      - name: Login to GHCR
        uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
      - name: Extract metadata
        id: meta
        uses: docker/metadata-action@v5
        with:
          images: ${{ env.REGISTRY }}/${{ env.IMAGE_PREFIX }}-${{ matrix.service }}
          tags: |
            type=ref,event=branch
            type=sha,prefix=
            type=raw,value=latest,enable=${{ github.ref == 'refs/heads/main' }}
      - name: Build and push
        uses: docker/build-push-action@v5
        with:
          context: .
          file: src/Services/JobRecon.${{ matrix.service }}/Dockerfile
          push: true
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
          cache-from: type=gha
          cache-to: type=gha,mode=max

  build-frontend-image:
    runs-on: ubuntu-latest
    needs: [test-frontend]
    if: github.event_name == 'push'
    permissions:
      contents: read
      packages: write
    steps:
      - uses: actions/checkout@v4
      - name: Download Frontend Build
        uses: actions/download-artifact@v4
        with:
          name: frontend-build
          path: frontend/dist/
      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v3
      - name: Login to GHCR
        uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
      - name: Build and push
        uses: docker/build-push-action@v5
        with:
          context: frontend
          push: true
          tags: ${{ env.REGISTRY }}/${{ env.IMAGE_PREFIX }}-frontend:${{ github.sha }}
          cache-from: type=gha
          cache-to: type=gha,mode=max
```

### D.3 Local Deployment Workflow (Self-Hosted Runner)

```yaml
# .github/workflows/cd-local.yml
name: Deploy to Local k3s

on:
  workflow_run:
    workflows: ["CI Pipeline"]
    types: [completed]
    branches: [main]
  workflow_dispatch:
    inputs:
      environment:
        description: 'Target environment'
        required: true
        default: 'staging'
        type: choice
        options:
          - staging
          - production

jobs:
  deploy:
    runs-on: self-hosted  # Self-hosted runner in home lab
    if: ${{ github.event.workflow_run.conclusion == 'success' || github.event_name == 'workflow_dispatch' }}
    environment: ${{ github.event.inputs.environment || 'staging' }}
    steps:
      - uses: actions/checkout@v4

      - name: Set image tag
        id: vars
        run: echo "sha_short=$(git rev-parse --short HEAD)" >> $GITHUB_OUTPUT

      - name: Login to GHCR
        run: |
          echo "${{ secrets.GITHUB_TOKEN }}" | docker login ghcr.io -u ${{ github.actor }} --password-stdin

      - name: Pull latest images
        run: |
          docker pull ghcr.io/${{ github.repository_owner }}/jobrecon-identity:${{ steps.vars.outputs.sha_short }}
          docker pull ghcr.io/${{ github.repository_owner }}/jobrecon-profile:${{ steps.vars.outputs.sha_short }}
          docker pull ghcr.io/${{ github.repository_owner }}/jobrecon-gateway:${{ steps.vars.outputs.sha_short }}
          docker pull ghcr.io/${{ github.repository_owner }}/jobrecon-frontend:${{ steps.vars.outputs.sha_short }}

      - name: Deploy with Helm
        run: |
          helm upgrade --install jobrecon ./deploy/helm/jobrecon \
            --namespace jobrecon-${{ github.event.inputs.environment || 'staging' }} \
            --create-namespace \
            --set global.image.tag=${{ steps.vars.outputs.sha_short }} \
            --set global.image.registry=ghcr.io/${{ github.repository_owner }} \
            --values ./deploy/helm/jobrecon/values-${{ github.event.inputs.environment || 'staging' }}.yaml \
            --wait --timeout 10m

      - name: Verify deployment
        run: |
          kubectl rollout status deployment/jobrecon-gateway -n jobrecon-${{ github.event.inputs.environment || 'staging' }}
          kubectl rollout status deployment/jobrecon-identity -n jobrecon-${{ github.event.inputs.environment || 'staging' }}

      - name: Run smoke tests
        run: |
          GATEWAY_URL=$(kubectl get ingress jobrecon-gateway -n jobrecon-${{ github.event.inputs.environment || 'staging' }} -o jsonpath='{.spec.rules[0].host}')
          curl -f https://$GATEWAY_URL/health || exit 1
```

### D.4 OpenAPI Breaking Change Detection

```yaml
# .github/workflows/openapi-check.yml
name: OpenAPI Breaking Change Check

on:
  pull_request:
    paths:
      - 'src/Services/**/Controllers/**'
      - 'src/Services/**/Endpoints/**'
      - 'src/Shared/JobRecon.Contracts/**'

jobs:
  check-breaking-changes:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Generate Current OpenAPI Specs
        run: |
          dotnet build --configuration Release
          mkdir -p api-specs/current
          dotnet swagger tofile --output api-specs/current/identity.json src/Services/JobRecon.Identity/bin/Release/net10.0/JobRecon.Identity.dll v1

      - name: Get Base Branch Specs
        run: |
          git checkout ${{ github.base_ref }}
          dotnet build --configuration Release
          mkdir -p api-specs/base
          dotnet swagger tofile --output api-specs/base/identity.json src/Services/JobRecon.Identity/bin/Release/net10.0/JobRecon.Identity.dll v1 || true

      - name: Check for Breaking Changes
        uses: oasdiff/oasdiff-action/breaking@main
        with:
          base: api-specs/base/identity.json
          revision: api-specs/current/identity.json
          fail-on-diff: true

      - name: Comment on PR
        if: failure()
        uses: actions/github-script@v7
        with:
          script: |
            github.rest.issues.createComment({
              issue_number: context.issue.number,
              owner: context.repo.owner,
              repo: context.repo.repo,
              body: '⚠️ **Breaking API Changes Detected**\n\nThis PR contains breaking changes to the API. Please review and ensure clients are updated accordingly.'
            })
```

### D.5 Release Workflow

```yaml
# .github/workflows/release.yml
name: Release

on:
  push:
    tags:
      - 'v*.*.*'

jobs:
  release:
    runs-on: ubuntu-latest
    permissions:
      contents: write
      packages: write
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Get version from tag
        id: version
        run: echo "version=${GITHUB_REF#refs/tags/v}" >> $GITHUB_OUTPUT

      - name: Build and Push Release Images
        run: |
          # Tag all images with release version
          for service in identity profile jobs-crawler jobs-processor gateway frontend; do
            docker pull ghcr.io/${{ github.repository_owner }}/jobrecon-${service}:main
            docker tag ghcr.io/${{ github.repository_owner }}/jobrecon-${service}:main \
              ghcr.io/${{ github.repository_owner }}/jobrecon-${service}:${{ steps.version.outputs.version }}
            docker push ghcr.io/${{ github.repository_owner }}/jobrecon-${service}:${{ steps.version.outputs.version }}
          done

      - name: Generate Changelog
        id: changelog
        uses: orhun/git-cliff-action@v3
        with:
          config: cliff.toml
          args: --latest --strip header

      - name: Create GitHub Release
        uses: softprops/action-gh-release@v1
        with:
          body: ${{ steps.changelog.outputs.content }}
          draft: false
          prerelease: ${{ contains(github.ref, '-rc') || contains(github.ref, '-beta') }}

      - name: Package Helm Chart
        run: |
          helm package ./deploy/helm/jobrecon --version ${{ steps.version.outputs.version }}

      - name: Upload Helm Chart to Release
        uses: softprops/action-gh-release@v1
        with:
          files: jobrecon-${{ steps.version.outputs.version }}.tgz
```

### D.6 Self-Hosted Runner Setup (for Home Lab)

```bash
#!/bin/bash
# deploy/k3s/setup/install-runner.sh

# Install GitHub Actions self-hosted runner on k3s node

RUNNER_VERSION="2.311.0"
GITHUB_REPO="your-username/JobRecon"

# Create runner directory
mkdir -p /opt/actions-runner && cd /opt/actions-runner

# Download runner
curl -o actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz -L \
  https://github.com/actions/runner/releases/download/v${RUNNER_VERSION}/actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz

tar xzf actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz

# Configure runner (requires GitHub token)
./config.sh --url https://github.com/${GITHUB_REPO} \
  --token ${RUNNER_TOKEN} \
  --name "homelab-k3s" \
  --labels "self-hosted,linux,k3s,homelab" \
  --work "_work" \
  --runasservice

# Install and start service
sudo ./svc.sh install
sudo ./svc.sh start

# Verify runner has kubectl access
kubectl get nodes
```

### D.7 GitHub Repository Configuration

```yaml
# Branch Protection Rules (configure in GitHub UI or via API)

main:
  required_reviews: 1
  dismiss_stale_reviews: true
  require_code_owner_reviews: true
  required_status_checks:
    strict: true
    contexts:
      - "lint-backend"
      - "lint-frontend"
      - "test-backend"
      - "test-frontend"
      - "build-images"
  enforce_admins: false
  restrictions: null

develop:
  required_reviews: 1
  required_status_checks:
    strict: false
    contexts:
      - "lint-backend"
      - "lint-frontend"
      - "test-backend"
      - "test-frontend"
```

```yaml
# .github/dependabot.yml
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "weekly"
    groups:
      dotnet:
        patterns:
          - "Microsoft.*"
          - "System.*"

  - package-ecosystem: "npm"
    directory: "/frontend"
    schedule:
      interval: "weekly"
    groups:
      react:
        patterns:
          - "react*"
          - "@tanstack/*"

  - package-ecosystem: "docker"
    directory: "/"
    schedule:
      interval: "weekly"

  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
```

```
# .github/CODEOWNERS
# Default owners
* @your-username

# Backend services
/src/Services/ @your-username
/src/Shared/ @your-username

# Frontend
/frontend/ @your-username

# Infrastructure
/deploy/ @your-username
/.github/ @your-username
```

---

This architecture provides a comprehensive foundation for a portfolio-worthy microservices system. The design emphasizes:

1. **Security-first approach** with strict secrets management - no credentials in code, ever
2. **Clear bounded contexts** with well-defined service responsibilities
3. **Event-driven decoupling** for scalability and resilience
4. **Cost optimization** by running expensive AI workloads locally on k3s
5. **Deployment flexibility** with abstractions enabling local/cloud portability
6. **API-first development** with OpenAPI specs driving TypeScript client generation
7. **Modern frontend** with React, TypeScript, and type-safe API integration
8. **Enterprise identity** ready for EntraID and external provider integration
9. **Observable and operable** with built-in monitoring and tracing
10. **Professional CI/CD** with GitHub Actions for automated builds, tests, and deployments
11. **GitOps-ready** with container images pushed to GitHub Container Registry
