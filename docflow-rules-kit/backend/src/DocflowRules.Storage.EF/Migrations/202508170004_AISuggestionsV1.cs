using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocflowRules.Storage.EF.Migrations
{
    public partial class AISuggestionsV1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SuggestedTests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RuleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    CoverageDeltaJson = table.Column<string>(type: "TEXT", nullable: false),
                    Score = table.Column<double>(type: "REAL", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Model = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_SuggestedTests", x => x.Id); }
            );
            migrationBuilder.CreateIndex(
                name: "IX_SuggestedTests_RuleId_Hash",
                table: "SuggestedTests",
                columns: new[] {"RuleId", "Hash"}
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SuggestedTests");
        }
    }
}
