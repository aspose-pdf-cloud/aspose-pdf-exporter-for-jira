using Microsoft.EntityFrameworkCore.Migrations;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Migrations
{
    public partial class AddAccessLogJsonStat : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*migrationBuilder.AlterColumn<string>(
                name: "stats",
                table: "access_log",
                type: "jsonb",
                nullable: true,
                comment: "statistics json",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "statistics json");
            */
            migrationBuilder.DropColumn(
                name: "stats",
                table: "access_log"
                );
            
            migrationBuilder.AddColumn<string>(
                name: "stats",
                table: "access_log",
                type: "jsonb",
                nullable: true,
                comment: "statistics json");

            migrationBuilder.CreateIndex(
                name: "IX_access_log_stats",
                table: "access_log",
                column: "stats")
                .Annotation("Npgsql:IndexMethod", "gin");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_access_log_stats",
                table: "access_log");

            /*migrationBuilder.AlterColumn<string>(
                name: "stats",
                table: "access_log",
                type: "text",
                nullable: true,
                comment: "statistics json",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true,
                oldComment: "statistics json");
                */
            migrationBuilder.DropColumn(
                name: "stats",
                table: "access_log"
                );

            migrationBuilder.AddColumn<string>(
                name: "stats",
                table: "access_log",
                nullable: true,
                comment: "statistics json");
        }
    }
}
