using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Infrastructure.Persistence.Configurations
{
    public class DocumentConfiguration : IEntityTypeConfiguration<Domain.Entities.Document>
    {
        public void Configure(EntityTypeBuilder<Domain.Entities.Document> builder)
        {
            builder.ToTable("documents");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.Title).IsRequired().HasMaxLength(500);

            // TenantId is not filtered/enforced yet (Phase 8), but the index is
            // added now -- it costs nothing today and every tenant-scoped query
            // added later benefits from it immediately.
            builder.HasIndex(d => d.TenantId);

            builder.HasMany(d => d.Versions)
                .WithOne(v => v.Document)
                .HasForeignKey(v => v.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
