using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTwoFactorSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TwoFactorChannel",
                table: "users",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TwoFactorEnabled",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "two_factor_challenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Channel = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    OtpHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OtpSalt = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    OtpEncrypted = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    DeliveryStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_two_factor_challenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_two_factor_challenges_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_two_factor_challenges_UserId",
                table: "two_factor_challenges",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_two_factor_challenges_UserId_Purpose",
                table: "two_factor_challenges",
                columns: new[] { "UserId", "Purpose" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "two_factor_challenges");

            migrationBuilder.DropColumn(
                name: "TwoFactorChannel",
                table: "users");

            migrationBuilder.DropColumn(
                name: "TwoFactorEnabled",
                table: "users");
        }
    }
}
