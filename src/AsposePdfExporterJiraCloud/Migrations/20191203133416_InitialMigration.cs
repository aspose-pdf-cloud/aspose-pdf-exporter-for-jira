using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "hilo_seq",
                incrementBy: 10);

            migrationBuilder.CreateTable(
                name: "client_state",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false, comment: "client states")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SequenceHiLo),
                    text = table.Column<string>(nullable: true, comment: "client states")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_state", x => x.id);
                },
                comment: "client states");

            migrationBuilder.CreateTable(
                name: "client_registration",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false, comment: "data payload with important tenant information")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SequenceHiLo),
                    key = table.Column<string>(nullable: true, comment: "data payload with important tenant information"),
                    client_key = table.Column<string>(nullable: true, comment: "data payload with important tenant information"),
                    public_key = table.Column<string>(nullable: true, comment: "data payload with important tenant information"),
                    shared_secret = table.Column<string>(nullable: false, comment: "data payload with important tenant information"),
                    server_version = table.Column<string>(nullable: true, comment: "data payload with important tenant information"),
                    plugins_version = table.Column<string>(nullable: true, comment: "data payload with important tenant information"),
                    base_url = table.Column<string>(nullable: true, comment: "data payload with important tenant information"),
                    product_type = table.Column<string>(nullable: true, comment: "data payload with important tenant information"),
                    description = table.Column<string>(nullable: true, comment: "data payload with important tenant information"),
                    client_state_id = table.Column<int>(nullable: false, comment: "data payload with important tenant information"),
                    created = table.Column<DateTime>(type: "timestamp", nullable: true, comment: "data payload with important tenant information"),
                    modified = table.Column<DateTime>(type: "timestamp", nullable: true, comment: "data payload with important tenant information"),
                    deleted = table.Column<DateTime>(type: "timestamp", nullable: true, comment: "data payload with important tenant information")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_registration", x => x.id);
                    table.ForeignKey(
                        name: "FK_client_registration_client_state_client_state_id",
                        column: x => x.client_state_id,
                        principalTable: "client_state",
                        principalColumn: "id");
                },
                comment: "data payload with important tenant information");

            migrationBuilder.CreateTable(
                name: "access_log",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false, comment: "client access log")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SequenceHiLo),
                    url = table.Column<string>(nullable: true, comment: "client access log"),
                    client_reg_id = table.Column<int>(nullable: false, comment: "client access log"),
                    accessed = table.Column<DateTime>(type: "timestamp", nullable: true, comment: "client access log")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_access_log", x => x.id);
                    table.ForeignKey(
                        name: "FK_access_log_client_registration_client_reg_id",
                        column: x => x.client_reg_id,
                        principalTable: "client_registration",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "client access log");

            migrationBuilder.CreateTable(
                name: "report_file",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false, comment: "report files")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SequenceHiLo),
                    unique_id = table.Column<string>(nullable: true, comment: "report files"),
                    report_type = table.Column<string>(nullable: true, comment: "report files"),
                    content_type = table.Column<string>(nullable: true, comment: "report files"),
                    file_name = table.Column<string>(nullable: true, comment: "report files"),
                    storage_file_name = table.Column<string>(nullable: true, comment: "report files"),
                    storage_folder = table.Column<string>(nullable: true, comment: "report files"),
                    file_size = table.Column<long>(nullable: false, comment: "report files"),
                    client_reg_id = table.Column<int>(nullable: false, comment: "report files"),
                    created = table.Column<DateTime>(type: "timestamp", nullable: true, comment: "report files"),
                    accessed = table.Column<DateTime>(type: "timestamp", nullable: true, comment: "report files"),
                    expired = table.Column<DateTime>(type: "timestamp", nullable: true, comment: "report files")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_file", x => x.id);
                    table.ForeignKey(
                        name: "FK_report_file_client_registration_client_reg_id",
                        column: x => x.client_reg_id,
                        principalTable: "client_registration",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "report files");

            migrationBuilder.InsertData(
                table: "client_state",
                columns: new[] { "id", "text" },
                values: new object[,]
                {
                    { 1, "app-installed" },
                    { 2, "app-uninstalled" },
                    { 3, "app-enabled" },
                    { 4, "app-disabled" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_access_log_client_reg_id",
                table: "access_log",
                column: "client_reg_id");

            migrationBuilder.CreateIndex(
                name: "IX_client_registration_client_key",
                table: "client_registration",
                column: "client_key");

            migrationBuilder.CreateIndex(
                name: "IX_client_registration_client_state_id",
                table: "client_registration",
                column: "client_state_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_file_client_reg_id",
                table: "report_file",
                column: "client_reg_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_file_unique_id",
                table: "report_file",
                column: "unique_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access_log");

            migrationBuilder.DropTable(
                name: "report_file");

            migrationBuilder.DropTable(
                name: "client_registration");

            migrationBuilder.DropTable(
                name: "client_state");

            migrationBuilder.DropSequence(
                name: "hilo_seq");
        }
    }
}
