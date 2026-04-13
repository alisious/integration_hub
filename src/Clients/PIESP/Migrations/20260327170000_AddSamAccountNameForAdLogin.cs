using IntegrationHub.PIESP.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntegrationHub.PIESP.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(PiespDbContext))]
    [Migration("20260327170000_AddSamAccountNameForAdLogin")]
    public partial class AddSamAccountNameForAdLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SamAccountName",
                schema: "piesp",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_SamAccountName",
                schema: "piesp",
                table: "Users",
                column: "SamAccountName",
                unique: true,
                filter: "[SamAccountName] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_SamAccountName",
                schema: "piesp",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SamAccountName",
                schema: "piesp",
                table: "Users");
        }
    }
}
