using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Swn.Workflow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskTransitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskTransitionDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromTaskDefinitionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToTaskDefinitionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequiredDecisionOutcomeKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskTransitionDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskTransitionDefinitions_TaskDefinitions_FromTaskDefinitionId",
                        column: x => x.FromTaskDefinitionId,
                        principalTable: "TaskDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskTransitionDefinitions_TaskDefinitions_ToTaskDefinitionId",
                        column: x => x.ToTaskDefinitionId,
                        principalTable: "TaskDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskTransitionDefinitions_FromTaskDefinitionId",
                table: "TaskTransitionDefinitions",
                column: "FromTaskDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskTransitionDefinitions_ToTaskDefinitionId",
                table: "TaskTransitionDefinitions",
                column: "ToTaskDefinitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskTransitionDefinitions");
        }
    }
}
