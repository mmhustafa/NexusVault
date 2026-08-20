using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusVault.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Infrastructure.Persistence.Configurations
{
    public class ChunkConfiguration : IEntityTypeConfiguration<Chunk>
    {
        public void Configure(EntityTypeBuilder<Chunk> builder)
        {
            builder.ToTable("chunks");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Text).IsRequired();
            builder.Property(c => c.ContentHash).IsRequired().HasMaxLength(64);
            builder.Property(c => c.SectionHeading).HasMaxLength(500);

            builder.HasIndex(c => new { c.DocumentVersionId, c.ChunkIndex }).IsUnique();

            // Every retrieval query for Phase 3+ will scope by tenant --
            // index exists now so it's ready the moment filtering starts.
            builder.HasIndex(c => new { c.TenantId, c.DocumentVersionId });

            builder.HasOne(c => c.Embedding)
                .WithOne(e => e.Chunk)
                .HasForeignKey<Embedding>(e => e.ChunkId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
