using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rag.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessLevelChangeLogsAndAudits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccessLevelChangeLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldLevel = table.Column<int>(type: "integer", nullable: false),
                    NewLevel = table.Column<int>(type: "integer", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessLevelChangeLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: true),
                    PerformedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAuditLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessLevelChangeLogs");

            migrationBuilder.DropTable(
                name: "DocumentAuditLogs");
        }
    }
}
