using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Auth.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropEmailConfirmed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "email_confirmed",
                table: "users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "email_confirmed",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
