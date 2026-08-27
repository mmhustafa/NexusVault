using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusVault.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenTenantId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_UserId",
                table: "refresh_tokens");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "refresh_tokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId_TenantId",
                table: "refresh_tokens",
                columns: new[] { "UserId", "TenantId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_UserId_TenantId",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "refresh_tokens");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId",
                table: "refresh_tokens",
                column: "UserId");
        }
    }
}
