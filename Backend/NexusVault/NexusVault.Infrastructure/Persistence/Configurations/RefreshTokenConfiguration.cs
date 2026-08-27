using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusVault.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("refresh_tokens");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(128);
            builder.HasIndex(t => t.TokenHash).IsUnique();
            builder.HasIndex(t => new { t.UserId, t.TenantId });

            // IsActive is a computed property, not a mapped column -- EF Core
            // must be told explicitly to ignore it, or it tries (and fails) to
            // map a bool with no setter to a column.
            builder.Ignore(t => t.IsActive);
        }
    }
}
