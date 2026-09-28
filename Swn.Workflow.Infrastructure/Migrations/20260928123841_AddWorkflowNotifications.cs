using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Swn.Workflow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TaskInstanceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RecipientUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    RecipientEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    TriggerKey = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AttemptCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastAttemptAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastError = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowNotifications_TaskInstances_TaskInstanceId",
                        column: x => x.TaskInstanceId,
                        principalTable: "TaskInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkflowNotifications_WorkflowInstances_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalTable: "WorkflowInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowNotifications_Status",
                table: "WorkflowNotifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowNotifications_TaskInstanceId",
                table: "WorkflowNotifications",
                column: "TaskInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowNotifications_WorkflowInstanceId_RecipientUserId_Type_TriggerKey",
                table: "WorkflowNotifications",
                columns: new[] { "WorkflowInstanceId", "RecipientUserId", "Type", "TriggerKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowNotifications");
        }
    }
}
