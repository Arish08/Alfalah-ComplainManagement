using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowDynamicFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowNodeFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FieldType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Placeholder = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    OptionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowNodeFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowNodeFields_WorkflowNodes_WorkflowNodeId",
                        column: x => x.WorkflowNodeId,
                        principalTable: "WorkflowNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowFieldResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComplaintId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowNodeFieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowFieldResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowFieldResponses_Complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "Complaints",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkflowFieldResponses_Users_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkflowFieldResponses_WorkflowNodeFields_WorkflowNodeFieldId",
                        column: x => x.WorkflowNodeFieldId,
                        principalTable: "WorkflowNodeFields",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkflowFieldResponses_WorkflowNodes_WorkflowNodeId",
                        column: x => x.WorkflowNodeId,
                        principalTable: "WorkflowNodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkflowFieldResponses_WorkflowTasks_WorkflowTaskId",
                        column: x => x.WorkflowTaskId,
                        principalTable: "WorkflowTasks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowFieldResponses_ComplaintId",
                table: "WorkflowFieldResponses",
                column: "ComplaintId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowFieldResponses_SubmittedByUserId",
                table: "WorkflowFieldResponses",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowFieldResponses_WorkflowNodeFieldId",
                table: "WorkflowFieldResponses",
                column: "WorkflowNodeFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowFieldResponses_WorkflowNodeId",
                table: "WorkflowFieldResponses",
                column: "WorkflowNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowFieldResponses_WorkflowTaskId_WorkflowNodeFieldId",
                table: "WorkflowFieldResponses",
                columns: new[] { "WorkflowTaskId", "WorkflowNodeFieldId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowNodeFields_WorkflowNodeId_DisplayOrder",
                table: "WorkflowNodeFields",
                columns: new[] { "WorkflowNodeId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowNodeFields_WorkflowNodeId_FieldKey",
                table: "WorkflowNodeFields",
                columns: new[] { "WorkflowNodeId", "FieldKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowFieldResponses");

            migrationBuilder.DropTable(
                name: "WorkflowNodeFields");
        }
    }
}
