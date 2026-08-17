using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusVault.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Infrastructure.Persistence.Configurations
{
    public class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
    {
        public void Configure(EntityTypeBuilder<DocumentVersion> builder)
        {
            builder.ToTable("document_versions");

            builder.HasKey(v => v.Id);

            builder.Property(v => v.OriginalFileName).IsRequired().HasMaxLength(500);
            builder.Property(v => v.ContentType).IsRequired().HasMaxLength(200);
            builder.Property(v => v.StoragePath).IsRequired().HasMaxLength(1000);
            builder.Property(v => v.ContentHash).IsRequired().HasMaxLength(64); // hex SHA-256

            builder.Property(v => v.Status).HasConversion<string>().HasMaxLength(50);

            // The idempotency guarantee at the DB level: a given Document can
            // never have two version rows with the same content hash. Even if
            // application-level idempotency checks are ever bypassed (a race, a
            // bug), this constraint is the last line of defense against
            // duplicate processing of identical content.
            builder.HasIndex(v => new { v.DocumentId, v.ContentHash }).IsUnique();

            builder.HasIndex(v => new { v.TenantId, v.DocumentId });
            builder.HasIndex(v => v.Status);

            builder.HasOne(v => v.Text)
                .WithOne(t => t.DocumentVersion)
                .HasForeignKey<DocumentText>(t => t.DocumentVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
