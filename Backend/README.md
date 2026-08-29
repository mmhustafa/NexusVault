# NexusVault Backend

ASP.NET Core (.NET 10) REST API built with Clean Architecture. Provides multi-tenant document management, semantic search, and AI-powered question answering by orchestrating a companion Python AI service for all model inference.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Project Structure](#project-structure)
- [API Endpoints](#api-endpoints)
  - [AuthController](#authcontroller----apiauth)
  - [DocumentsController](#documentscontroller----apidocuments)
  - [SearchController](#searchcontroller----apisearch)
  - [AskController](#askcontroller----apiask)
  - [InvitationsController](#invitationscontroller----apiinvitations)
- [AI Service Integration](#ai-service-integration)
- [Key Flows](#key-flows)

---

## Architecture Overview

```
Client
  |
  v
NexusVault.Api          <- Controllers, JWT auth, Swagger, Hangfire Dashboard
  |
  +-- NexusVault.Application  <- Business services, interfaces, DTOs (no infra deps)
  |
  +-- NexusVault.Domain       <- Entities and enums only (no dependencies)
  |
  +-- NexusVault.Infrastructure
          +-- PostgreSQL + pgvector  (EF Core)
          +-- ASP.NET Core Identity  (users, roles)
          +-- Hangfire               (background jobs, Postgres-backed)
          +-- LocalFileStorage       (raw file persistence)
          +-- PdfTextExtractor / DocxTextExtractor
          +-- HTTP clients -------->  AI Service (Python/FastAPI)
                                        /chunk  /embed  /rerank  /rag-synthesize
```

Layer dependency rule: `Api` -> `Application` <- `Infrastructure`. `Domain` has no outward dependencies.

---

## Project Structure

```
Backend/NexusVault/
+-- NexusVault.slnx
|
+-- NexusVault.Api/                        <- Presentation
|   +-- Program.cs                         # DI wiring + middleware pipeline
|   +-- appsettings.json                   # Default config (JWT, DB, AI service, storage)
|   +-- Controllers/
|       +-- AuthController.cs              # 6 auth endpoints
|       +-- DocumentsController.cs         # 4 document endpoints
|       +-- SearchController.cs            # 1 search endpoint
|       +-- AskController.cs               # 1 RAG Q&A endpoint
|       +-- InvitationsController.cs       # 1 invitation endpoint
|
+-- NexusVault.Application/                <- Business logic
|   +-- Services/
|   |   +-- DocumentIngestionService.cs    # Upload, versioning, dedup, job scheduling
|   |   +-- SearchService.cs               # Dense / sparse / hybrid + optional rerank
|   |   +-- AskService.cs                  # Retrieve -> rerank -> synthesize pipeline
|   |   +-- ContentHasher.cs              # SHA-256 deduplication helper
|   +-- Interfaces/                        # IAuthService, IDocumentRepository, IChunkingService ...
|   +-- DTOs/                             # All request/response records
|
+-- NexusVault.Domain/                     <- Core domain (no external deps)
|   +-- Entities/                          # Document, DocumentVersion, Chunk, Embedding, Tenant ...
|   +-- Enums/
|       +-- DocumentVersionStatus.cs       # Pending | Processing | Ready | Failed
|
+-- NexusVault.Infrastructure/             <- External concerns
    +-- Identity/                          # AuthService, JwtTokenService, CurrentTenant
    +-- Persistence/                       # NexusVaultDbContext, Repositories, EF Configurations
    +-- AiService/                         # HTTP clients for /chunk /embed /rerank /rag-synthesize
    +-- Jobs/                              # ProcessDocumentVersionJob (Hangfire) + scheduler
    +-- TextExtraction/                    # PdfTextExtractor, DocxTextExtractor, resolver
    +-- LocalFileStorage.cs               # Saves raw uploads to local disk
```

---

## API Endpoints

All routes are prefixed with `/api`.

**Auth levels used in the tables below:**

| Level | Meaning |
|-------|---------|
| Anonymous | No token required |
| Authenticated | Any valid JWT (no tenant claim needed) |
| Tenant-scoped | JWT with a `tenant_id` claim required |
| Admin | Tenant-scoped JWT + `Admin` role |


---

## AuthController — `/api/auth`

Handles user registration, login, workspace management, and token lifecycle. No tenant context is required for most operations — this controller is the entry point before a tenant is selected.

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/register` | Anonymous | Create a new user account |
| POST | `/login` | Anonymous | Authenticate; returns either a ready token pair or a tenant-selection token |
| POST | `/select-tenant` | Authenticated | Exchange a tenant-selection token for a full tenant-scoped token pair |
| POST | `/create-workspace` | Authenticated | Create a new tenant and become its Admin |
| POST | `/accept-invitation` | Authenticated | Join an existing tenant via an invitation token |
| POST | `/refresh` | Anonymous | Rotate an access + refresh token pair |



### POST `/api/auth/register`

**Request**
```json
{ "email": "user@example.com", "password": "password123" }
```

**Response `200 OK`**

New users have no tenant yet, so the response always requires tenant selection:
```json
{
  "requiresTenantSelection": true,
  "tokens": null,
  "tenantSelectionToken": "<jwt>",
  "availableTenants": []
}
```



### POST `/api/auth/login`

**Request**
```json
{ "email": "user@example.com", "password": "password123" }
```

**Response `200 OK` — single-tenant user (ready immediately)**
```json
{
  "requiresTenantSelection": false,
  "tokens": {
    "accessToken": "<jwt>",
    "refreshToken": "<opaque>",
    "accessTokenExpiresAt": "2026-08-30T00:00:00Z"
  },
  "tenantSelectionToken": null,
  "availableTenants": null
}
```

**Response `200 OK` — no tenant or multi-tenant user (must call `/select-tenant` next)**
```json
{
  "requiresTenantSelection": true,
  "tokens": null,
  "tenantSelectionToken": "<jwt>",
  "availableTenants": [
    { "tenantId": "<guid>", "tenantName": "Acme Corp", "role": "Admin" }
  ]
}
```


### POST `/api/auth/select-tenant`

**Request**
```json
{ "tenantId": "<guid>" }
```

**Response `200 OK`**
```json
{
  "accessToken": "<jwt>",
  "refreshToken": "<opaque>",
  "accessTokenExpiresAt": "2026-08-30T00:00:00Z"
}
```


### POST `/api/auth/create-workspace`

**Request**
```json
{ "workspaceName": "My Company Docs" }
```

**Response `200 OK`** — caller is automatically made Admin of the new tenant
```json
{
  "accessToken": "<jwt>",
  "refreshToken": "<opaque>",
  "accessTokenExpiresAt": "2026-08-30T00:00:00Z"
}
```


### POST `/api/auth/accept-invitation`

The authenticated user's email must match the invitation's target email.

**Request**
```json
{ "invitationToken": "<opaque>" }
```

**Response `200 OK`**
```json
{
  "accessToken": "<jwt>",
  "refreshToken": "<opaque>",
  "accessTokenExpiresAt": "2026-08-30T00:00:00Z"
}
```


### POST `/api/auth/refresh`

The old refresh token is revoked immediately upon rotation.

**Request**
```json
{ "refreshToken": "<opaque>" }
```

**Response `200 OK`**
```json
{
  "accessToken": "<jwt>",
  "refreshToken": "<opaque>",
  "accessTokenExpiresAt": "2026-08-30T00:00:00Z"
}
```


---

## DocumentsController — `/api/documents`

Manages document uploads and ingestion status. All endpoints require a **Tenant-scoped** JWT. Upload endpoints are further restricted to the **Admin** role.

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/` | Admin | Upload a new document (PDF or DOCX, max 25 MB) |
| GET | `/{documentVersionId}/status` | Tenant-scoped | Poll ingestion status for a document version |
| POST | `/{documentId}/versions` | Admin | Upload a new version of an existing document |
| GET | `/{documentId}/versions` | Tenant-scoped | List full version history for a document |



### POST `/api/documents`

Accepts `multipart/form-data`. Supported types: PDF, DOCX. Max size: 25 MB.

**Request (form fields)**

| Field | Type | Description |
|-------|------|-------------|
| `file` | File | The document file |
| `title` | string | Display title for the document |

**Response `202 Accepted`**
```json
{
  "documentId": "<guid>",
  "documentVersionId": "<guid>",
  "status": "Pending",
  "wasDuplicate": false
}
```


### GET `/api/documents/{documentVersionId}/status`

**Response `200 OK`**
```json
{
  "documentVersionId": "<guid>",
  "status": "Ready",
  "attemptCount": 1,
  "errorMessage": null,
  "createdAt": "2026-08-29T20:00:00Z",
  "readyAt": "2026-08-29T20:00:45Z"
}
```

`status` lifecycle: `Pending` -> `Processing` -> `Ready` | `Failed`

**Response `404 Not Found`** — version does not exist or belongs to a different tenant.


### POST `/api/documents/{documentId}/versions`

Accepts `multipart/form-data`. Same file type and size rules as the initial upload.
Idempotent — uploading identical content returns the existing version without re-ingesting.

**Request (form fields)**

| Field | Type | Description |
|-------|------|-------------|
| `file` | File | The replacement file (PDF or DOCX) |

**Response `202 Accepted` — new version**
```json
{
  "documentId": "<guid>",
  "documentVersionId": "<guid>",
  "status": "Pending",
  "wasDuplicate": false
}
```

**Response `202 Accepted` — duplicate content (no new version created)**
```json
{
  "documentId": "<guid>",
  "documentVersionId": "<existing-version-guid>",
  "status": "Ready",
  "wasDuplicate": true
}
```

**Response `404 Not Found`** — document does not exist or belongs to a different tenant.



### GET `/api/documents/{documentId}/versions`

**Response `200 OK`**
```json
[
  {
    "versionId": "<guid>",
    "versionNumber": 2,
    "isCurrent": true,
    "status": "Ready",
    "createdAt": "2026-08-29T21:00:00Z",
    "readyAt": "2026-08-29T21:00:50Z"
  },
  {
    "versionId": "<guid>",
    "versionNumber": 1,
    "isCurrent": false,
    "status": "Ready",
    "createdAt": "2026-08-29T20:00:00Z",
    "readyAt": "2026-08-29T20:00:45Z"
  }
]
```

**Response `404 Not Found`** — document does not exist or belongs to a different tenant.


---

## SearchController — `/api/search`

Searches tenant documents using dense (vector), sparse (full-text), or hybrid retrieval. Requires a **Tenant-scoped** JWT.

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/` | Tenant-scoped | Search tenant documents |



### POST `/api/search`

**Request**
```json
{
  "query": "What is the cancellation policy?",
  "topK": 5,
  "documentId": null,
  "mode": "Hybrid",
  "fusion": "Rrf",
  "rerank": true,
  "includeArchived": false
}
```

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `query` | string | required | The search query text |
| `topK` | int? | `5` | Max results to return (1–20) |
| `documentId` | Guid? | `null` | Scope search to a single document |
| `mode` | enum | `Dense` | `Dense` (vector), `Sparse` (full-text), `Hybrid` (both) |
| `fusion` | enum | `Rrf` | `Rrf` (Reciprocal Rank Fusion) or `WeightedSum` — applies only when `mode = Hybrid` |
| `rerank` | bool | `false` | Apply cross-encoder reranking after retrieval |
| `includeArchived` | bool | `false` | Include chunks from non-current document versions |

**Response `200 OK`**
```json
[
  {
    "chunkId": "<guid>",
    "documentId": "<guid>",
    "documentTitle": "Employee Handbook",
    "text": "Cancellations must be submitted 30 days in advance...",
    "pageNumber": 12,
    "sectionHeading": null,
    "score": 0.94
  }
]
```


---

## AskController — `/api/ask`

Answers natural-language questions using RAG over tenant documents. Requires a **Tenant-scoped** JWT.

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/` | Tenant-scoped | Answer a question using RAG |



### POST `/api/ask`

Internally runs **Hybrid search + rerank** (`topK = 5`), then calls `/rag-synthesize` on the AI service. All cited chunk IDs are validated against the retrieved set — the endpoint rejects any IDs the LLM fabricates.

**Request**
```json
{
  "query": "When does the warranty expire?",
  "documentId": null
}
```

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `query` | string | required | The question to answer |
| `documentId` | Guid? | `null` | Scope to a single document |

**Response `200 OK`**
```json
{
  "answer": "The warranty expires 12 months after the original purchase date.",
  "citations": [
    {
      "chunkId": "<guid>",
      "documentId": "<guid>",
      "documentTitle": "Warranty Policy",
      "pageNumber": 3,
      "sectionHeading": null
    }
  ]
}
```

---

## InvitationsController — `/api/invitations`

Creates workspace invitations. Requires a **Tenant-scoped** JWT with the **Admin** role.

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/` | Admin | Create an invitation to join the current tenant |



### POST `/api/invitations`

Invitations expire after 7 days. The `rawToken` is stored as a SHA-256 hash and cannot be retrieved again after this response — share it with the invitee immediately.

**Request**
```json
{ "email": "colleague@example.com", "role": "User" }
```

| Field | Type | Options |
|-------|------|---------|
| `email` | string | Target user email address |
| `role` | string? | `"User"` (default) or `"Admin"` |

**Response `200 OK`**
```json
{
  "invitationId": "<guid>",
  "rawToken": "<opaque>",
  "expiresAt": "2026-09-05T22:00:00Z"
}
```
---

## AI Service Integration

The backend communicates with the external FastAPI AI service via dedicated typed HTTP clients configured in `NexusVault.Infrastructure.AiService`. The endpoints consumed by the backend are:

| Service Client | Target AI Endpoint | Usage in Backend |
|----------------|-------------------|------------------|
| `HttpChunkingService` | `POST /chunk` | `ProcessDocumentVersionJob` (Hangfire): splits extracted document text into structure-aware chunks |
| `HttpEmbeddingService` | `POST /embed` | `ProcessDocumentVersionJob` (batch chunk embedding) & `SearchService` (query vector generation) |
| `HttpRerankingService` | `POST /rerank` | `SearchService` & `AskService`: re-scores candidate chunks with cross-encoder |
| `HttpRagSynthesisService` | `POST /rag-synthesize` | `AskService`: produces grounded QA answers citing context chunks |


---

## Key Flows

### Document Ingestion (async via Hangfire)

```
POST /api/documents  (or POST /api/documents/{id}/versions)
  |
  +-- Validate content type (PDF / DOCX) and size (<= 25 MB)
  +-- Compute SHA-256 hash -> check for duplicate content
  +-- Save raw file to LocalFileStorage
  +-- Persist Document + DocumentVersion (status = Pending)
  +-- Enqueue Hangfire job
          |
          +-- ProcessDocumentVersionJob
                +-- Extract text  (PDF -> PdfPig | DOCX -> OpenXml)
                +-- POST /chunk  -> AI Service
                +-- POST /embed  -> AI Service
                +-- Persist Chunks + Embeddings (pgvector)
                +-- Set status = Ready  (or Failed on error)
```

### Search

```
POST /api/search
  +-- Dense:   embed query -> pgvector ANN search
  +-- Sparse:  PostgreSQL full-text (tsvector) search
  +-- Hybrid:  both above -> fuse scores (RRF or WeightedSum)
  +-- Rerank:  POST /rerank -> AI Service (cross-encoder, optional)
```

### Ask (RAG)

```
POST /api/ask
  +-- SearchAsync (Hybrid + Rerank, topK=5)
  +-- POST /rag-synthesize -> AI Service (Groq LLM)
  +-- Validate cited chunk IDs against retrieved set
  +-- Return answer + verified citations
```
