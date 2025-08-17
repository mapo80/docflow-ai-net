using Microsoft.EntityFrameworkCore;

namespace DocflowRules.Storage.EF;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<RuleFunction> RuleFunctions => Set<RuleFunction>();
    public DbSet<RuleTestCase> RuleTestCases => Set<RuleTestCase>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AppUserRole> UserRoles => Set<AppUserRole>();
    public DbSet<SuggestedTest> SuggestedTests => Set<SuggestedTest>();
    public DbSet<LlmModel> LlmModels => Set<LlmModel>();
    public DbSet<LlmSettings> LlmSettings => Set<LlmSettings>();
    public DbSet<GgufDownloadJob> GgufDownloadJobs => Set<GgufDownloadJob>();
    public DbSet<TestSuite> TestSuites => Set<TestSuite>();
    public DbSet<TestTag> TestTags => Set<TestTag>();
    public DbSet<RuleTestCaseTag> RuleTestCaseTags => Set<RuleTestCaseTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RuleFunction>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.CodeHash).HasMaxLength(128);
        });
        modelBuilder.Entity<RuleTestCase>(e =>
        {
            e.HasIndex(x => new { x.RuleFunctionId, x.Name }).IsUnique();
        });
        
        modelBuilder.Entity<AppUserRole>(e => { e.HasKey(x => new { x.UserId, x.Role }); });
        modelBuilder.Entity<LlmSettings>(e => { e.HasKey(x => x.Id); });

    }
}
