using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApiClients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "api_clients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ClientId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "api_client_workspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_client_workspaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_api_client_workspaces_api_clients_ApiClientId",
                        column: x => x.ApiClientId,
                        principalTable: "api_clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_api_client_workspaces_workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "api_client_workspace_roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiClientWorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_client_workspace_roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_api_client_workspace_roles_api_client_workspaces_ApiClientW~",
                        column: x => x.ApiClientWorkspaceId,
                        principalTable: "api_client_workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_api_client_workspace_roles_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_api_client_workspace_roles_ApiClientWorkspaceId_RoleId",
                table: "api_client_workspace_roles",
                columns: new[] { "ApiClientWorkspaceId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_api_client_workspace_roles_RoleId",
                table: "api_client_workspace_roles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_api_client_workspaces_ApiClientId_WorkspaceId",
                table: "api_client_workspaces",
                columns: new[] { "ApiClientId", "WorkspaceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_api_client_workspaces_WorkspaceId",
                table: "api_client_workspaces",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_api_clients_ClientId",
                table: "api_clients",
                column: "ClientId",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_api_clients_Name",
                table: "api_clients",
                column: "Name",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_client_workspace_roles");

            migrationBuilder.DropTable(
                name: "api_client_workspaces");

            migrationBuilder.DropTable(
                name: "api_clients");
        }
    }
}
