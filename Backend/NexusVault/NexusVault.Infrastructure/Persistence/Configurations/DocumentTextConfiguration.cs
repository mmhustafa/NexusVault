using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusVault.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Infrastructure.Persistence.Configurations
{
    public class DocumentTextConfiguration : IEntityTypeConfiguration<DocumentText>
    {
        public void Configure(EntityTypeBuilder<DocumentText> builder)
        {
            builder.ToTable("document_texts");

            builder.HasKey(t => t.DocumentVersionId);

            builder.Property(t => t.Content).IsRequired();
            builder.Property(t => t.ExtractionMethod).IsRequired().HasMaxLength(100);
        }
    }
}
