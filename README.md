# NexusVault

**A private, multi-tenant semantic search and RAG platform for organizational knowledge.**

## Overview

Organizations accumulate huge volumes of internal documentation — policies, technical references, reports, manuals — that's technically searchable but practically opaque, because keyword search fails the moment a question doesn't match a document's exact wording. Someone searching *"remote work policy"* gets nothing useful if the actual document says *"employees may work from home up to three days per week."*

NexusVault solves this properly: documents are ingested, understood, and indexed by *meaning*, not just words — so people can ask real questions in plain language and get accurate, cited answers grounded in their organization's own documents, not guesses from a general-purpose chatbot.

## Features

### Document Ingestion
Upload a PDF or Word document, and NexusVault automatically extracts the text, breaks it into meaningful pieces, and indexes it — all in the background, so uploading feels instant even though the real work takes a little time.

### Semantic Search
Ask a question in your own words and NexusVault finds the relevant passage even if it doesn't share your exact vocabulary — understanding what you mean, not just matching what you typed.

### Keyword Search
Precise, exact-term search for the cases meaning-based search isn't built for — error codes, identifiers, exact phrases.

### Hybrid Search
Combines semantic and keyword search together, since different questions need different kinds of search, and the best results often come from blending both.

### Relevance Reranking
After finding the most likely matches, a second, more careful pass re-examines the strongest candidates to make sure the best answer actually rises to the top — the same two-stage "find candidates, then double-check" approach used by real production search systems.

### Grounded Question-Answering
Ask a direct question and get a natural-language answer — grounded entirely in the organization's own documents, with every claim traceable back to the exact source document, page, and section it came from. If the answer isn't in the documents, it says so honestly instead of guessing.

### Workspaces
Every organization gets its own private workspace, with complete data isolation between them — one company can never see another's documents, under any circumstance. A single person can belong to more than one workspace at once, just like real tools such as Slack or Notion — create your own workspace, join someone else's, and switch between them without needing separate accounts.

### Users & Roles
Every workspace member has a role that determines what they can do — Admins can upload, manage, and organize documents, while every member can search and ask questions. Someone can hold different roles in different workspaces at the same time — an Admin of their own team's workspace, but just a member of another team's.

### Invitations
Admins bring new people into a workspace by sending them an invitation. The invitee accepts it using their own account — whether they're brand new to NexusVault or already have an account elsewhere — and joins with exactly the role the Admin assigned them. Invitations are single-use and expire automatically, so access stays deliberate and controlled.

### Document Versioning
Uploading a revised version of a document doesn't lose history — the old version stays available and searchable right up until the new version is fully processed and verified, so the document never "disappears," and past versions remain accessible for reference even after being superseded.
