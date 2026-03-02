using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityFederation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "identity_sources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identity_sources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "identity_source_links",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdentitySourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalIdentity = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identity_source_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_identity_source_links_identity_sources_IdentitySourceId",
                        column: x => x.IdentitySourceId,
                        principalTable: "identity_sources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_identity_source_links_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "identity_source_oidc_configs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdentitySourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Authority = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ClientId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ClientSecret = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identity_source_oidc_configs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_identity_source_oidc_configs_identity_sources_IdentitySourc~",
                        column: x => x.IdentitySourceId,
                        principalTable: "identity_sources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_identity_source_links_IdentitySourceId_ExternalIdentity",
                table: "identity_source_links",
                columns: new[] { "IdentitySourceId", "ExternalIdentity" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_identity_source_links_UserId",
                table: "identity_source_links",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_identity_source_oidc_configs_IdentitySourceId",
                table: "identity_source_oidc_configs",
                column: "IdentitySourceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_identity_sources_Name",
                table: "identity_sources",
                column: "Name",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "identity_source_links");

            migrationBuilder.DropTable(
                name: "identity_source_oidc_configs");

            migrationBuilder.DropTable(
                name: "identity_sources");
        }
    }
}
