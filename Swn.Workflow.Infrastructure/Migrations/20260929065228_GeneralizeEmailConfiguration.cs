using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Swn.Workflow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GeneralizeEmailConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "SmtpConfigurations",
                newName: "EmailConfigurations");

            migrationBuilder.RenameColumn(
                name: "Host",
                table: "EmailConfigurations",
                newName: "SmtpHost");

            migrationBuilder.RenameColumn(
                name: "Port",
                table: "EmailConfigurations",
                newName: "SmtpPort");

            migrationBuilder.RenameColumn(
                name: "Security",
                table: "EmailConfigurations",
                newName: "SmtpSecurity");

            migrationBuilder.RenameColumn(
                name: "FromAddress",
                table: "EmailConfigurations",
                newName: "SenderAddress");

            migrationBuilder.RenameColumn(
                name: "FromName",
                table: "EmailConfigurations",
                newName: "SenderName");

            migrationBuilder.AddColumn<int>(
                name: "Transport",
                table: "EmailConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EwsMailboxAddress",
                table: "EmailConfigurations",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "UseAutodiscover",
                table: "EmailConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "EwsServiceUrl",
                table: "EmailConfigurations",
                type: "TEXT",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Transport",
                table: "EmailConfigurations");

            migrationBuilder.DropColumn(
                name: "EwsMailboxAddress",
                table: "EmailConfigurations");

            migrationBuilder.DropColumn(
                name: "UseAutodiscover",
                table: "EmailConfigurations");

            migrationBuilder.DropColumn(
                name: "EwsServiceUrl",
                table: "EmailConfigurations");

            migrationBuilder.RenameColumn(
                name: "SmtpHost",
                table: "EmailConfigurations",
                newName: "Host");

            migrationBuilder.RenameColumn(
                name: "SmtpPort",
                table: "EmailConfigurations",
                newName: "Port");

            migrationBuilder.RenameColumn(
                name: "SmtpSecurity",
                table: "EmailConfigurations",
                newName: "Security");

            migrationBuilder.RenameColumn(
                name: "SenderAddress",
                table: "EmailConfigurations",
                newName: "FromAddress");

            migrationBuilder.RenameColumn(
                name: "SenderName",
                table: "EmailConfigurations",
                newName: "FromName");

            migrationBuilder.RenameTable(
                name: "EmailConfigurations",
                newName: "SmtpConfigurations");
        }
    }
}