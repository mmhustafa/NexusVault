using Microsoft.EntityFrameworkCore;
using NexusVault.Application.DTOs;
using NexusVault.Application.Interfaces;
using Pgvector;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Infrastructure.Persistence.Repositories
{
    public class ChunkSearchRepository : IChunkSearchRepository
    {
        private readonly NexusVaultDbContext _db;

        public ChunkSearchRepository(NexusVaultDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<ChunkSearchResult>> FindNearestAsync(
            float[] queryVector,
            Guid tenantId,
            int topK,
            Guid? documentId = null,
            bool includeArchived = false,
            CancellationToken ct = default)
        {
            var pgVector = new Vector(queryVector);

            var rows = await _db.Database.SqlQuery<ChunkSearchRow>($"""
            SELECT
                c."Id" AS "ChunkId",
                dv."DocumentId" AS "DocumentId",
                d."Title" AS "DocumentTitle",
                c."Text" AS "Text",
                c."PageNumber" AS "PageNumber",
                c."SectionHeading" AS "SectionHeading",
                (e."Vector" <=> {pgVector}) AS "Distance"
            FROM "embeddings" e
            JOIN "chunks" c ON c."Id" = e."ChunkId"
            JOIN "document_versions" dv ON dv."Id" = c."DocumentVersionId"
            JOIN "documents" d ON d."Id" = dv."DocumentId"
            WHERE c."TenantId" = {tenantId}
              AND ({documentId}::uuid IS NULL OR dv."DocumentId" = {documentId}::uuid)
              AND ({includeArchived} = true OR dv."IsCurrent" = true)
            ORDER BY e."Vector" <=> {pgVector}
            LIMIT {topK}
            """).ToListAsync(ct);

            return rows
                .Select(r => new ChunkSearchResult(
                    r.ChunkId,
                    r.DocumentId,
                    r.DocumentTitle,
                    r.Text,
                    r.PageNumber,
                    r.SectionHeading,
                    r.Distance))
                .ToList();
        }

        public async Task<IReadOnlyList<ChunkFullTextResult>> FindByFullTextAsync(
            string queryText,
            Guid tenantId,
            int topK,
            Guid? documentId = null,
            bool includeArchived = false,
            CancellationToken ct = default)
        {
            var rows = await _db.Database.SqlQuery<ChunkFullTextSearchRow>($"""
            SELECT
                c."Id" AS "ChunkId",
                dv."DocumentId" AS "DocumentId",
                d."Title" AS "DocumentTitle",
                c."Text" AS "Text",
                c."PageNumber" AS "PageNumber",
                c."SectionHeading" AS "SectionHeading",
                ts_rank_cd(c.search_vector, plainto_tsquery('english', {queryText})) AS "Rank"
            FROM "chunks" c
            JOIN "document_versions" dv ON dv."Id" = c."DocumentVersionId"
            JOIN "documents" d ON d."Id" = dv."DocumentId"
            WHERE c."TenantId" = {tenantId}
              AND ({documentId}::uuid IS NULL OR dv."DocumentId" = {documentId}::uuid)
              AND ({includeArchived} = true OR dv."IsCurrent" = true)
              AND c.search_vector @@ plainto_tsquery('english', {queryText})
            ORDER BY ts_rank_cd(c.search_vector, plainto_tsquery('english', {queryText})) DESC
            LIMIT {topK}
            """).ToListAsync(ct);

            return rows
                .Select(r => new ChunkFullTextResult(
                    r.ChunkId,
                    r.DocumentId,
                    r.DocumentTitle,
                    r.Text,
                    r.PageNumber,
                    r.SectionHeading,
                    r.Rank))
                .ToList();
        }
    }
}
