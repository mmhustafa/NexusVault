using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusVault.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChunksSearchVector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE chunks ADD COLUMN search_vector tsvector
                GENERATED ALWAYS AS (to_tsvector('english', "Text")) STORED;
                """);

            migrationBuilder.Sql("""
                CREATE INDEX "IX_chunks_search_vector" ON chunks USING GIN (search_vector);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_chunks_search_vector";""");
            migrationBuilder.Sql("""ALTER TABLE chunks DROP COLUMN IF EXISTS search_vector;""");
        }
    }
}
