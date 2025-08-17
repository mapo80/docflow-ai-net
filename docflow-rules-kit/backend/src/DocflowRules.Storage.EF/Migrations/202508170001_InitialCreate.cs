using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocflowRules.Storage.EF.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RuleFunctions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    IsBuiltin = table.Column<bool>(type: "INTEGER", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    CodeHash = table.Column<string>(type: "TEXT", nullable: false),
                    ReadsCsv = table.Column<string>(type: "TEXT", nullable: true),
                    WritesCsv = table.Column<string>(type: "TEXT", nullable: true),
                    Owner = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_RuleFunctions", x => x.Id); }
            );
            migrationBuilder.CreateIndex(name: "IX_RuleFunctions_Name", table: "RuleFunctions", column: "Name", unique: true);

            migrationBuilder.CreateTable(
                name: "RuleTestCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RuleFunctionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    InputJson = table.Column<string>(type: "TEXT", nullable: false),
                    ExpectJson = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Suite = table.Column<string>(type: "TEXT", nullable: true),
                    TagsCsv = table.Column<string>(type: "TEXT", nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_RuleTestCases", x => x.Id); }
            );
            migrationBuilder.CreateIndex(name: "IX_RuleTestCases_RuleFunctionId_Name", table: "RuleTestCases", columns: new[] { "RuleFunctionId", "Name" }, unique: true);

            migrationBuilder.CreateTable(
                name: "TestSuites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_TestSuites", x => x.Id); }
            );
            migrationBuilder.CreateIndex(name: "IX_TestSuites_Name", table: "TestSuites", column: "Name", unique: true);

            migrationBuilder.CreateTable(
                name: "TestTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_TestTags", x => x.Id); }
            );
            migrationBuilder.CreateIndex(name: "IX_TestTags_Name", table: "TestTags", column: "Name", unique: true);

            migrationBuilder.CreateTable(
                name: "RuleTestCaseTags",
                columns: table => new
                {
                    RuleTestCaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TestTagId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleTestCaseTags", x => new { x.RuleTestCaseId, x.TestTagId });
                    table.ForeignKey(name: "FK_RuleTestCaseTags_RuleTestCases_RuleTestCaseId", column: x => x.RuleTestCaseId, principalTable: "RuleTestCases", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(name: "FK_RuleTestCaseTags_TestTags_TestTagId", column: x => x.TestTagId, principalTable: "TestTags", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                }
            );
            migrationBuilder.CreateIndex(name: "IX_RuleTestCaseTags_TestTagId", table: "RuleTestCaseTags", column: "TestTagId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "RuleTestCaseTags");
            migrationBuilder.DropTable(name: "TestSuites");
            migrationBuilder.DropTable(name: "TestTags");
            migrationBuilder.DropTable(name: "RuleTestCases");
            migrationBuilder.DropTable(name: "RuleFunctions");
        }
    }
}
