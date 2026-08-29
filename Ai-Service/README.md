# NexusVault AI Service

A stateless, Python/FastAPI microservice that exposes the AI inference capabilities used by the NexusVault backend. It handles document chunking, text embedding, candidate re-ranking, and RAG (Retrieval-Augmented Generation) answer synthesis. The service is intentionally stateless — it owns no database and performs no retrieval of its own; every call is a pure transformation of the inputs it receives.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Endpoints](#endpoints)
- [Project Structure](#project-structure)
- [Models Used](#models-used)
- [Configuration](#configuration)
---

## Architecture Overview

```
NexusVault Backend  ──►  /chunk           ──►  Chunker (structure-aware, token-bounded)
                    ──►  /embed           ──►  Embedder (all-mpnet-base-v2, batched)
                    ──►  /rerank          ──►  Reranker (cross-encoder/ms-marco-MiniLM-L-12-v2)
                    ──►  /rag-synthesize  ──►  RAG Synthesizer (Groq LLM, grounded QA)
                    ──►  /health          ──►  Liveness probe
```

The backend calls this service during two phases:

1. **Document ingestion** – `/chunk` then `/embed` to turn extracted text into searchable vectors.
2. **Query serving** – `/rerank` to re-score retrieved chunks, then `/rag-synthesize` to produce a cited answer.

---

## Endpoints

### `POST /chunk`

Splits raw document text into token-bounded, structure-aware chunks.

**Request**
```json
{
  "text": "<full extracted document text>",
  "max_tokens_per_chunk": 300
}
```

**Response**
```json
{
  "chunks": [
    {
      "chunk_index": 0,
      "text": "...",
      "page_number": 1,
      "section_heading": null
    }
  ]
}
```

- `max_tokens_per_chunk` must be between 50 and 1000 (capped at the embedding model's hard limit of 384).
- Splits first on paragraph breaks, then on sentence boundaries for oversized paragraphs.
- Page numbers are tracked using `\f` (form-feed) markers inserted by the backend's PDF extractor.

---

### `POST /embed`

Batch-embeds a list of text strings using `all-mpnet-base-v2`.

**Request**
```json
{
  "texts": ["chunk text 1", "chunk text 2"]
}
```

**Response**
```json
{
  "vectors": [[0.12, -0.34, "..."]],
  "model_name": "all-mpnet-base-v2",
  "model_version": "1",
  "dimensions": 768
}
```

- The model is loaded **once at startup** and reused across requests (not reloaded per call).

---

### `POST /rerank`

Scores a list of candidate (id, text) pairs against a query using a cross-encoder model.

**Request**
```json
{
  "query": "What is the refund policy?",
  "candidates": [
    { "id": "abc-123", "text": "Refunds are issued within 30 days..." },
    { "id": "xyz-789", "text": "Contact support for billing issues..." }
  ]
}
```

**Response**
```json
{
  "ranked": [
    { "id": "abc-123", "score": 8.74 },
    { "id": "xyz-789", "score": 1.23 }
  ]
}
```

- Results are returned **sorted by score descending**.
- Uses `cross-encoder/ms-marco-MiniLM-L-12-v2`.

---

### `POST /rag-synthesize`

Produces a grounded answer from pre-retrieved chunks using a Groq-hosted LLM.

**Request**
```json
{
  "query": "What are the payment terms?",
  "chunks": [
    { "id": "chunk-1", "text": "Payment is due within 30 days of invoice." },
    { "id": "chunk-2", "text": "Late fees apply after 60 days." }
  ]
}
```

**Response**
```json
{
  "answer": "Payment is due within 30 days of invoice, with late fees applying after 60 days.",
  "cited_chunk_ids": ["chunk-1", "chunk-2"]
}
```

- The LLM is instructed to answer **only** from the provided excerpts and to never fabricate chunk IDs.
- Uses `llama-3.3-70b-versatile` by default (overridable via `GROQ_MODEL` env var).
- Temperature is set to `0.0` for deterministic, reproducible answers.

---

### `GET /health`

Simple liveness probe. Returns `{"status": "ok"}`.

---

## Project Structure

```
Ai-Service/
├── main.py                  # FastAPI app entry point; mounts all routers
├── requirements.txt         # Pinned Python dependencies
├── .env                     # Local env vars (not committed)
│
├── Models/
│   └── schemas.py           # Pydantic request/response models for all endpoints
│
├── Routers/
│   ├── chunking.py          # POST /chunk
│   ├── embedding.py         # POST /embed
│   ├── reranking.py         # POST /rerank
│   └── rag.py               # POST /rag-synthesize
│
└── Services/
    ├── chunker.py           # Structure-aware chunking logic
    ├── embedder.py          # SentenceTransformer model wrapper (loaded once)
    ├── reranker.py          # CrossEncoder model wrapper (pure scoring)
    └── rag.py               # Groq client + grounded QA prompt
```

---

## Models Used

| Purpose    | Model                                    | Library                 |
|------------|------------------------------------------|-------------------------|
| Embedding  | `all-mpnet-base-v2`                      | `sentence-transformers` |
| Re-ranking | `cross-encoder/ms-marco-MiniLM-L-12-v2` | `sentence-transformers` |
| LLM (RAG)  | `llama-3.3-70b-versatile` (via Groq)    | `groq`                  |

Both local models are loaded once at process startup and shared across all requests.

---

## Configuration

Create a `.env` file (or set environment variables) before starting the service:

| Variable       | Required | Default                    | Description                     |
|----------------|----------|----------------------------|---------------------------------|
| `GROQ_API_KEY` | **Yes**  | —                          | Groq API key for LLM synthesis  |
| `GROQ_MODEL`   | No       | `llama-3.3-70b-versatile`  | Groq model name to use for RAG  |

---
