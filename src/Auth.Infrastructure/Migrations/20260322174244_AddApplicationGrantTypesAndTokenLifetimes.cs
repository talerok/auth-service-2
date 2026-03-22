using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationGrantTypesAndTokenLifetimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessTokenLifetimeMinutes",
                table: "applications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "GrantTypes",
                table: "applications",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[\"authorization_code\", \"refresh_token\"]'::jsonb");

            migrationBuilder.AddColumn<int>(
                name: "RefreshTokenLifetimeMinutes",
                table: "applications",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessTokenLifetimeMinutes",
                table: "applications");

            migrationBuilder.DropColumn(
                name: "GrantTypes",
                table: "applications");

            migrationBuilder.DropColumn(
                name: "RefreshTokenLifetimeMinutes",
                table: "applications");
        }
    }
}
