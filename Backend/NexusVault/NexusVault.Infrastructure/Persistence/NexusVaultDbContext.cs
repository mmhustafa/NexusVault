using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NexusVault.Domain.Entities;
using NexusVault.Infrastructure.Identity;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Text;

namespace NexusVault.Infrastructure.Persistence
{
    public class NexusVaultDbContext : IdentityUserContext<ApplicationUser, Guid>
    {
        public NexusVaultDbContext(DbContextOptions<NexusVaultDbContext> options) : base(options) { }

        public DbSet<Domain.Entities.Document> Documents => Set<Domain.Entities.Document>();
        public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
        public DbSet<DocumentText> DocumentTexts => Set<DocumentText>();
        public DbSet<Chunk> Chunks => Set<Chunk>();
        public DbSet<Embedding> Embeddings => Set<Embedding>();

        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<TenantUser> TenantUsers => Set<TenantUser>();
        public DbSet<Invitation> Invitations => Set<Invitation>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasPostgresExtension("vector");
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(NexusVaultDbContext).Assembly);
        }
    }
}
