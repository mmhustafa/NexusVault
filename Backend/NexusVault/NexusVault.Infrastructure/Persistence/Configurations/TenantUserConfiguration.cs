using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusVault.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusVault.Infrastructure.Persistence.Configurations
{
    public class TenantUserConfiguration : IEntityTypeConfiguration<TenantUser>
    {
        public void Configure(EntityTypeBuilder<TenantUser> builder)
        {
            builder.ToTable("tenant_users");

            builder.HasKey(u => u.Id); 

            builder.Property(u => u.Email).IsRequired().HasMaxLength(320);
            builder.Property(u => u.Role).IsRequired().HasMaxLength(50);

            builder.HasIndex(u => u.UserId);

            // A user can't have two membership rows for the same tenant --
            // the actual constraint that makes "many tenants per user" safe
            // rather than accidentally duplicable.
            builder.HasIndex(u => new { u.UserId, u.TenantId }).IsUnique();
        }
    }
}
