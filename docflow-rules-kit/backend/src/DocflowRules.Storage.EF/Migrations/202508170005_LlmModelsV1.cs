using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocflowRules.Storage.EF.Migrations
{
    public partial class LlmModelsV1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LlmModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ModelPathOrId = table.Column<string>(type: "TEXT", nullable: true),
                    Endpoint = table.Column<string>(type: "TEXT", nullable: true),
                    ApiKey = table.Column<string>(type: "TEXT", nullable: true),
                    ContextSize = table.Column<int>(type: "INTEGER", nullable: true),
                    Threads = table.Column<int>(type: "INTEGER", nullable: true),
                    BatchSize = table.Column<int>(type: "INTEGER", nullable: true),
                    MaxTokens = table.Column<int>(type: "INTEGER", nullable: true),
                    Temperature = table.Column<double>(type: "REAL", nullable: true),
                    WarmupOnStart = table.Column<bool>(type: "INTEGER", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_LlmModels", x => x.Id); }
            );

            migrationBuilder.CreateTable(
                name: "LlmSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    ActiveModelId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TurboProfile = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_LlmSettings", x => x.Id); }
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "LlmModels");
            migrationBuilder.DropTable(name: "LlmSettings");
        }
    }
}
