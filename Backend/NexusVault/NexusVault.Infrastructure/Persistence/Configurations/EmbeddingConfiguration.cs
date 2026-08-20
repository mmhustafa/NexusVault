using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusVault.Domain.Entities;
using Pgvector;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Infrastructure.Persistence.Configurations
{
    public class EmbeddingConfiguration : IEntityTypeConfiguration<Embedding>
    {
        public void Configure(EntityTypeBuilder<Embedding> builder)
        {
            builder.ToTable("embeddings");

            // ChunkId is both the PK and the FK -- enforces the 1:1 relationship
            // at the database level, not just in application code.
            builder.HasKey(e => e.ChunkId);

            builder.Property(e => e.ModelName).IsRequired().HasMaxLength(200);
            builder.Property(e => e.ModelVersion).IsRequired().HasMaxLength(50);

            // pgvector column -- 768 dimensions matches all-mpnet-base-v2.
            var vectorComparer = new ValueComparer<float[]>(
            (a, b) => (a ?? Array.Empty<float>()).SequenceEqual(b ?? Array.Empty<float>()),
            a => a.Aggregate(0, (hash, val) => HashCode.Combine(hash, val)),
            a => a.ToArray());

            builder.Property(e => e.Vector)
                .HasColumnType("vector(768)")
                .HasConversion(
                    v => new Vector(v),
                    v => v.ToArray(),vectorComparer);

            // HNSW index for approximate nearest-neighbor search
            builder.HasIndex(e => e.Vector)
                .HasMethod("hnsw")
                .HasOperators("vector_cosine_ops");
        }
    }
}
