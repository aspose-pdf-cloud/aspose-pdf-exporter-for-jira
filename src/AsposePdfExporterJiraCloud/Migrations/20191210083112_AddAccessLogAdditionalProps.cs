using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Migrations
{
    public partial class AddAccessLogAdditionalProps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "finished",
                table: "access_log",
                type: "timestamp",
                nullable: true,
                comment: "finish time");

            migrationBuilder.AddColumn<string>(
                name: "message",
                table: "access_log",
                nullable: true,
                comment: "custom message");

            migrationBuilder.AddColumn<int>(
                name: "passed_sec",
                table: "access_log",
                nullable: true,
                comment: "process request seconds");

            migrationBuilder.AddColumn<string>(
                name: "stats",
                table: "access_log",
                nullable: true,
                comment: "statistics json");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "finished",
                table: "access_log");

            migrationBuilder.DropColumn(
                name: "message",
                table: "access_log");

            migrationBuilder.DropColumn(
                name: "passed_sec",
                table: "access_log");

            migrationBuilder.DropColumn(
                name: "stats",
                table: "access_log");
        }
    }
}
