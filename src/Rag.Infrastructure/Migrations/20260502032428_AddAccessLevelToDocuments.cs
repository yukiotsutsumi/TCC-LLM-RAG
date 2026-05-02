using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rag.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessLevelToDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "Documents",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "Documents");
        }
    }
}
