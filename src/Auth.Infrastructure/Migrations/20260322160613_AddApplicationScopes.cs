using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationScopes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "Scopes",
                table: "applications",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Scopes",
                table: "applications");
        }
    }
}
