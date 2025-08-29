using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocflowRules.Storage.EF.Migrations
{
    public partial class GovernanceV1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(name: "Status", table: "RuleFunctions", type: "INTEGER", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<string>(name: "SemVersion", table: "RuleFunctions", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Signature", table: "RuleFunctions", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "BuiltinLocked", table: "RuleFunctions", type: "INTEGER", nullable: false, defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Status", table: "RuleFunctions");
            migrationBuilder.DropColumn(name: "SemVersion", table: "RuleFunctions");
            migrationBuilder.DropColumn(name: "Signature", table: "RuleFunctions");
            migrationBuilder.DropColumn(name: "BuiltinLocked", table: "RuleFunctions");
        }
    }
}
