using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Migrations
{
    public partial class AddErrorLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_access_log_client_registration_client_reg_id",
                table: "access_log");

            migrationBuilder.AddColumn<string>(
                name: "user_account_id",
                table: "client_registration",
                nullable: true,
                comment: "user_account_id");

            migrationBuilder.AlterColumn<int>(
                name: "client_reg_id",
                table: "access_log",
                nullable: true,
                comment: "client registration id",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "client registration id");

            migrationBuilder.CreateTable(
                name: "error_log",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false, comment: "client error log")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SequenceHiLo),
                    url = table.Column<string>(nullable: true, comment: "client error log"),
                    client_reg_id = table.Column<int>(nullable: true, comment: "client error log"),
                    message = table.Column<string>(nullable: true, comment: "client error log"),
                    trace = table.Column<string>(nullable: true, comment: "client error log"),
                    comment = table.Column<string>(nullable: true, comment: "client error log"),
                    occured = table.Column<DateTime>(type: "timestamp", nullable: true, comment: "client error log")
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
                name: "IX_error_log_client_reg_id",
                table: "error_log",
                column: "client_reg_id");

            migrationBuilder.AddForeignKey(
                name: "FK_access_log_client_registration_client_reg_id",
                table: "access_log",
                column: "client_reg_id",
                principalTable: "client_registration",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_access_log_client_registration_client_reg_id",
                table: "access_log");

            migrationBuilder.DropTable(
                name: "error_log");

            migrationBuilder.DropColumn(
                name: "user_account_id",
                table: "client_registration");

            migrationBuilder.AlterColumn<int>(
                name: "client_reg_id",
                table: "access_log",
                type: "integer",
                nullable: false,
                comment: "client registration id",
                oldClrType: typeof(int),
                oldNullable: true,
                oldComment: "client registration id");

            migrationBuilder.AddForeignKey(
                name: "FK_access_log_client_registration_client_reg_id",
                table: "access_log",
                column: "client_reg_id",
                principalTable: "client_registration",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
