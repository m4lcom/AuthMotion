using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthMotion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserForOtpAnd2FA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EmailVerificationToken",
                table: "Users",
                newName: "VerificationToken");

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationTokenExpiryTime",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VerificationTokenExpiryTime",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "VerificationToken",
                table: "Users",
                newName: "EmailVerificationToken");
        }
    }
}
