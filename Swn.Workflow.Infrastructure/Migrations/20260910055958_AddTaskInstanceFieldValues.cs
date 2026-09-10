using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Swn.Workflow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskInstanceFieldValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskInstanceFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TaskInstanceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskInstanceFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskInstanceFieldValues_TaskInstances_TaskInstanceId",
                        column: x => x.TaskInstanceId,
                        principalTable: "TaskInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskInstanceFieldValues_TaskInstanceId_Key",
                table: "TaskInstanceFieldValues",
                columns: new[] { "TaskInstanceId", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskInstanceFieldValues");
        }
    }
}
