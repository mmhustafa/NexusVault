using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusVault.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class initialcreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "document_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessingStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReadyAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_document_versions_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_texts",
                columns: table => new
                {
                    DocumentVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ExtractionMethod = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PageCount = table.Column<int>(type: "integer", nullable: true),
                    ExtractedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_texts", x => x.DocumentVersionId);
                    table.ForeignKey(
                        name: "FK_document_texts_document_versions_DocumentVersionId",
                        column: x => x.DocumentVersionId,
                        principalTable: "document_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_document_versions_DocumentId_ContentHash",
                table: "document_versions",
                columns: new[] { "DocumentId", "ContentHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_versions_Status",
                table: "document_versions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_document_versions_TenantId_DocumentId",
                table: "document_versions",
                columns: new[] { "TenantId", "DocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_documents_TenantId",
                table: "documents",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_texts");

            migrationBuilder.DropTable(
                name: "document_versions");

            migrationBuilder.DropTable(
                name: "documents");
        }
    }
}
