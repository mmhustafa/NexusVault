# NexusVault

**A private, multi-tenant semantic search and RAG platform for organizational knowledge.**

- [Backend Documentation ](Backend/README.md)
- [AI Service Documentation  ](Ai-Service/README.md)

## Overview

Organizations accumulate large volumes of internal documentation such as policies, technical references, reports, and manuals. Traditional keyword search often fails when a user's query does not use the same wording as the document.

For example, searching for *"remote work policy"* may not find a document stating that *"employees may work from home up to three days per week."*

NexusVault solves this by indexing documents based on meaning rather than keywords alone. Users can search in natural language and receive accurate, grounded answers based on their organization's own documents, with traceable citations instead of unsupported guesses.

## Features

| Feature | Description |
|---|---|
| **Document Ingestion** | Users can upload PDF and Word documents. NexusVault extracts text, divides it into meaningful chunks, generates searchable indexes, and processes the document in the background so uploads remain responsive. |
| **Semantic Search** | Finds relevant content based on meaning rather than requiring exact keyword matches. Users can ask questions using their own wording. |
| **Keyword Search** | Supports precise searches for exact terms, phrases, identifiers, and error codes where semantic search may not be sufficient. |
| **Hybrid Search** | Combines semantic and keyword search to provide stronger results for different types of queries. |
| **Relevance Reranking** | Performs a second-stage evaluation of the strongest search candidates to improve result ordering and ensure the most relevant passages appear first. |
| **Grounded Question-Answering** | Generates natural-language answers based entirely on the organization's documents. Claims can be traced to their source document, page, and section. If the answer is not supported by the available documents, the system responds without guessing. |
| **Workspaces** | Provides isolated private workspaces for organizations. Data is separated between workspaces, preventing one organization from accessing another's documents. Users can belong to multiple workspaces and switch between them using the same account. |
| **Users and Roles** | Each workspace member has a role defining their permissions. Admins can manage documents and workspace resources, while members can search documents and ask questions. A user may have different roles across different workspaces. |
| **Invitations** | Workspace Admins can invite users and assign their role. Invitations can be accepted by new or existing NexusVault users, are single-use, and expire automatically. |
| **Document Versioning** | Revised documents can be uploaded without losing previous versions. The existing version remains available and searchable until the new version has been fully processed and verified. Previous versions remain accessible for historical reference. |

## Core Platform Principles

NexusVault is designed around the following principles:

- **Meaning-based retrieval:** Documents are searched by semantic meaning, not only exact keywords.
- **Grounded answers:** Responses are based on the organization's own knowledge rather than unsupported model guesses.
- **Traceability:** Answers can be linked back to the original document and source location.
- **Tenant isolation:** Each organization's data remains isolated within its workspace.
- **Flexible access control:** Users can belong to multiple workspaces and have different roles in each one.
- **Reliable document updates:** New document versions are processed safely without temporarily removing the previous searchable version.
- **Production-style retrieval:** Hybrid search and reranking improve the quality and relevance of retrieved information.

## Summary

NexusVault is a private, multi-tenant platform that transforms organizational documents into a searchable knowledge system. It supports document ingestion, semantic and keyword search, hybrid retrieval, reranking, grounded question-answering with citations, workspace isolation, role-based access, invitations, and document versioning.

The platform allows organizations to retrieve information based on meaning, ask questions in natural language, and receive answers that remain grounded in their own documents while maintaining security, access control, and separation between organizations.
