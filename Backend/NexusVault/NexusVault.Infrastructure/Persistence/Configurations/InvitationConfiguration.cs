using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusVault.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Infrastructure.Persistence.Configurations
{
    public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
    {
        public void Configure(EntityTypeBuilder<Invitation> builder)
        {
            builder.ToTable("invitations");
            builder.HasKey(i => i.Id);

            builder.Property(i => i.Email).IsRequired().HasMaxLength(320);
            builder.Property(i => i.TokenHash).IsRequired().HasMaxLength(128);
            builder.Property(i => i.Role).IsRequired().HasMaxLength(50);

            // A given email shouldn't have two live (unaccepted) invitations to
            // the same tenant -- partial unique index, only enforced while
            // AcceptedAt is null.
            builder.HasIndex(i => new { i.TenantId, i.Email })
                .IsUnique()
                .HasFilter("\"AcceptedAt\" IS NULL");

            builder.HasIndex(i => i.TokenHash).IsUnique();
        }
    }
}
