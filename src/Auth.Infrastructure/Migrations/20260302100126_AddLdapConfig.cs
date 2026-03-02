using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLdapConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "identity_source_ldap_configs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdentitySourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Host = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    BaseDn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BindDn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BindPassword = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UseSsl = table.Column<bool>(type: "boolean", nullable: false),
                    SearchFilter = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identity_source_ldap_configs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_identity_source_ldap_configs_identity_sources_IdentitySourc~",
                        column: x => x.IdentitySourceId,
                        principalTable: "identity_sources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_identity_source_ldap_configs_IdentitySourceId",
                table: "identity_source_ldap_configs",
                column: "IdentitySourceId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "identity_source_ldap_configs");
        }
    }
}
