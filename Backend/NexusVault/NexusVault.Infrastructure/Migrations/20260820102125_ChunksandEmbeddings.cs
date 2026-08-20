using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace NexusVault.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChunksandEmbeddings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "chunks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChunkIndex = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    PageNumber = table.Column<int>(type: "integer", nullable: true),
                    SectionHeading = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chunks_document_versions_DocumentVersionId",
                        column: x => x.DocumentVersionId,
                        principalTable: "document_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "embeddings",
                columns: table => new
                {
                    ChunkId = table.Column<Guid>(type: "uuid", nullable: false),
                    Vector = table.Column<Vector>(type: "vector(768)", nullable: false),
                    ModelName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ModelVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Dimensions = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_embeddings", x => x.ChunkId);
                    table.ForeignKey(
                        name: "FK_embeddings_chunks_ChunkId",
                        column: x => x.ChunkId,
                        principalTable: "chunks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chunks_DocumentVersionId_ChunkIndex",
                table: "chunks",
                columns: new[] { "DocumentVersionId", "ChunkIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chunks_TenantId_DocumentVersionId",
                table: "chunks",
                columns: new[] { "TenantId", "DocumentVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_embeddings_Vector",
                table: "embeddings",
                column: "Vector")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "embeddings");

            migrationBuilder.DropTable(
                name: "chunks");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }
    }
}
