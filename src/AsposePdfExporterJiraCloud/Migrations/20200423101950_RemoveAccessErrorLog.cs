using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Migrations
{
    public partial class RemoveAccessErrorLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // delete archived records
            migrationBuilder.Sql("DELETE FROM error_log WHERE client_reg_id in (SELECT id FROM client_registration WHERE deleted is not null)");
            migrationBuilder.Sql("DELETE FROM access_log WHERE client_reg_id in (SELECT id FROM client_registration WHERE deleted is not null)");
            migrationBuilder.Sql("DELETE FROM client_registration where deleted is not null");

            migrationBuilder.DropTable(
                name: "access_log");

            migrationBuilder.DropTable(
                name: "error_log");

            migrationBuilder.DropColumn(
                name: "deleted",
                table: "client_registration");

            migrationBuilder.DropColumn(
                name: "modified",
                table: "client_registration");
            
            // Fake client
            migrationBuilder.InsertData(
                table: "client_registration",
                columns: new [] { "id", "client_key", "shared_secret", "client_state_id" },
                values: new object[,]
                {
                    { -1, "dummy client for unbounded error logs", "", 4},
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "deleted",
                table: "client_registration",
                type: "timestamp",
                nullable: true,
                comment: "Row deleted");

            migrationBuilder.AddColumn<DateTime>(
                name: "modified",
                table: "client_registration",
                type: "timestamp",
                nullable: true,
                comment: "Row modified");

            migrationBuilder.CreateTable(
                name: "access_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false, comment: "Unique ID")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SequenceHiLo),
                    accessed = table.Column<DateTime>(type: "timestamp", nullable: true, comment: "access time"),
                    client_reg_id = table.Column<int>(type: "integer", nullable: true, comment: "client registration id"),
                    finished = table.Column<DateTime>(type: "timestamp", nullable: true, comment: "finish time"),
                    message = table.Column<string>(type: "text", nullable: true, comment: "custom message"),
                    passed_sec = table.Column<int>(type: "integer", nullable: true, comment: "process request seconds"),
                    stats = table.Column<string>(type: "jsonb", nullable: true, comment: "statistics json"),
                    url = table.Column<string>(type: "text", nullable: true, comment: "accessed url")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_access_log", x => x.id);
                    table.ForeignKey(
                        name: "FK_access_log_client_registration_client_reg_id",
                        column: x => x.client_reg_id,
                        principalTable: "client_registration",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "client access log");

            migrationBuilder.CreateTable(
                name: "error_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false, comment: "Unique ID")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SequenceHiLo),
                    client_reg_id = table.Column<int>(type: "integer", nullable: true, comment: "client registration id"),
                    comment = table.Column<string>(type: "text", nullable: true, comment: "some additional text"),
                    message = table.Column<string>(type: "text", nullable: true, comment: "message"),
                    occured = table.Column<DateTime>(type: "timestamp", nullable: true, comment: "when error occured"),
                    trace = table.Column<string>(type: "text", nullable: true, comment: "trace"),
                    url = table.Column<string>(type: "text", nullable: true, comment: "accessed url")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_error_log", x => x.id);
                    table.ForeignKey(
                        name: "FK_error_log_client_registration_client_reg_id",
                        column: x => x.client_reg_id,
                        principalTable: "client_registration",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "client error log");

            migrationBuilder.CreateIndex(
                name: "IX_access_log_client_reg_id",
                table: "access_log",
                column: "client_reg_id");

            migrationBuilder.CreateIndex(
                name: "IX_access_log_stats",
                table: "access_log",
                column: "stats")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_error_log_client_reg_id",
                table: "error_log",
                column: "client_reg_id");
        }
    }
}
