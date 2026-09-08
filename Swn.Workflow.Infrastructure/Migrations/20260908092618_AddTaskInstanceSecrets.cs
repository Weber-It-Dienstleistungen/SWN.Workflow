using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Swn.Workflow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskInstanceSecrets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskInstanceSecrets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TaskInstanceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    EncryptedValue = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskInstanceSecrets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskInstanceSecrets_TaskInstances_TaskInstanceId",
                        column: x => x.TaskInstanceId,
                        principalTable: "TaskInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskInstanceSecrets_TaskInstanceId_Key",
                table: "TaskInstanceSecrets",
                columns: new[] { "TaskInstanceId", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskInstanceSecrets");
        }
    }
}
