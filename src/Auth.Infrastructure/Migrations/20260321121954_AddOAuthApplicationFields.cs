using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOAuthApplicationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HomepageUrl",
                table: "api_clients",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsConfidential",
                table: "api_clients",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "api_clients",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "PostLogoutRedirectUris",
                table: "api_clients",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<List<string>>(
                name: "RedirectUris",
                table: "api_clients",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "api_clients",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "ServiceAccount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HomepageUrl",
                table: "api_clients");

            migrationBuilder.DropColumn(
                name: "IsConfidential",
                table: "api_clients");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "api_clients");

            migrationBuilder.DropColumn(
                name: "PostLogoutRedirectUris",
                table: "api_clients");

            migrationBuilder.DropColumn(
                name: "RedirectUris",
                table: "api_clients");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "api_clients");
        }
    }
}
