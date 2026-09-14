using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Swn.Workflow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowInstanceRoleAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowInstanceRoleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoleKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowInstanceRoleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowInstanceRoleAssignments_WorkflowInstances_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalTable: "WorkflowInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstanceRoleAssignments_UserId",
                table: "WorkflowInstanceRoleAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstanceRoleAssignments_WorkflowInstanceId_RoleKey",
                table: "WorkflowInstanceRoleAssignments",
                columns: new[] { "WorkflowInstanceId", "RoleKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowInstanceRoleAssignments");
        }
    }
}
