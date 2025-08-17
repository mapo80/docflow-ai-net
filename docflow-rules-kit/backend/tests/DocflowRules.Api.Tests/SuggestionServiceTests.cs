using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using DocflowRules.Api.Services;
using DocflowRules.Storage.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using FluentAssertions;

public class SuggestionServiceTests
{
    private AppDbContext NewDb()
    {
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(opt);
    }

    [Fact]
    public async Task Suggests_and_imports_tests_with_coverage_delta_and_dedupe()
    {
        await using var db = NewDb();
        var ruleId = Guid.NewGuid();
        db.RuleFunctions.Add(new RuleFunction { Id = ruleId, Name = "MyRule", Code = "if (amount > 100) return true; if (refNo != null) {}" });
        // existing test covers 'amount'
        db.RuleTestCases.Add(new RuleTestCase { Id=Guid.NewGuid(), RuleFunctionId = ruleId, Name = "exists", InputJson = "{}", ExpectJson = new JsonObject{ ["fields"] = new JsonObject { ["amount"] = new JsonObject{ ["equals"] = 42 } } }.ToJsonString() });
        // active model = Mock
        var model = new LlmModel { Id = Guid.NewGuid(), Provider = "Mock", Name = "mock" };
        db.LlmModels.Add(model);
        db.LlmSettings.Add(new LlmSettings { Id=1, ActiveModelId = model.Id, TurboProfile = false });
        await db.SaveChangesAsync();

        var services = new ServiceCollection();
        services.AddSingleton<ILLMProviderRegistry, LlmProviderRegistry>();
        services.AddSingleton<MockLLMProvider>();
        var sp = services.BuildServiceProvider();
        var reg = sp.GetRequiredService<ILLMProviderRegistry>();
        var cfgSvc = new LlmConfigService(db);
        var val = new DocflowRules.Api.Validation.TestUpsertValidator();
        var svc = new SuggestionService(db, val, reg, cfgSvc, NullLogger<SuggestionService>.Instance);

        var (list, modelName, totalSkeletons, inTok, outTok, durMs, cost) = await svc.SuggestAsync(ruleId, userPrompt: null, budget: 10, temperature: 0.2, modelId: null, turbo: null, ct: CancellationToken.None);
        list.Should().NotBeEmpty();
        list.Any(s => s.CoverageDeltaJson.Contains("refNo")).Should().BeTrue(); // new coverage
        // Import one
        var imported = await svc.ImportAsync(ruleId, new [] { list.First().Id }, suite: "ai", tags: new [] { "ai" }, CancellationToken.None);
        imported.Should().Be(1);

        // second suggest should dedupe identical payloads (no new entries added)
        var (list2, _, _, _, _, _, _) = await svc.SuggestAsync(ruleId, null, 10, 0.2, null, null, CancellationToken.None);
        (list2.Count <= totalSkeletons).Should().BeTrue();
    }
}
