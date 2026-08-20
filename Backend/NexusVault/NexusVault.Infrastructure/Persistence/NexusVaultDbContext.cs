using Microsoft.EntityFrameworkCore;
using NexusVault.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Text;

namespace NexusVault.Infrastructure.Persistence
{
    public class NexusVaultDbContext : DbContext
    {
        public NexusVaultDbContext(DbContextOptions<NexusVaultDbContext> options) : base(options) { }

        public DbSet<Domain.Entities.Document> Documents => Set<Domain.Entities.Document>();
        public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
        public DbSet<DocumentText> DocumentTexts => Set<DocumentText>();
        public DbSet<Chunk> Chunks => Set<Chunk>();
        public DbSet<Embedding> Embeddings => Set<Embedding>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("vector");

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(NexusVaultDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
