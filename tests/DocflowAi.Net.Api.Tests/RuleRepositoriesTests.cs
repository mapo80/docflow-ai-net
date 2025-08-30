using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Rules.Models;
using DocflowAi.Net.Api.Rules.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DocflowAi.Net.Api.Tests;

public class RuleRepositoriesTests
{
    [Fact]
    public void Repositories_handle_crud_operations()
    {
        using var conn = new SqliteConnection("Filename=:memory:");
        conn.Open();
        var opts = new DbContextOptionsBuilder<JobDbContext>().UseSqlite(conn).Options;
        using var db = new JobDbContext(opts);
        db.Database.EnsureCreated();

        var ruleRepo = new RuleFunctionRepository(db);
        var caseRepo = new RuleTestCaseRepository(db);
        var suiteRepo = new TestSuiteRepository(db);
        var tagRepo = new TestTagRepository(db);
        var sugRepo = new SuggestedTestRepository(db);
        var linkRepo = new RuleTestCaseTagRepository(db);

        var rule = new RuleFunction { Name = "r", Code = "c", CodeHash = "h" };
        ruleRepo.Add(rule);
        ruleRepo.SaveChanges();
        ruleRepo.GetAll().Should().HaveCount(1);
        ruleRepo.GetByName("r")!.Id.Should().Be(rule.Id);
        rule.Code = "c2";
        ruleRepo.Update(rule);
        ruleRepo.SaveChanges();
        ruleRepo.GetById(rule.Id)!.Code.Should().Be("c2");

        var suite = new TestSuite { Name = "suite" };
        suiteRepo.Add(suite);
        suiteRepo.SaveChanges();
        suite.Color = "red";
        suiteRepo.Update(suite);
        suiteRepo.SaveChanges();
        suiteRepo.GetById(suite.Id)!.Color.Should().Be("red");
        suiteRepo.Delete(suite.Id);
        suiteRepo.SaveChanges();
        suiteRepo.GetAll().Should().BeEmpty();

        var tag = new TestTag { Name = "tag" };
        tagRepo.Add(tag);
        tagRepo.SaveChanges();
        tag.Color = "blue";
        tagRepo.Update(tag);
        tagRepo.SaveChanges();
        tagRepo.GetByName("tag")!.Color.Should().Be("blue");

        var testCase = new RuleTestCase { RuleFunctionId = rule.Id, Name = "case", InputJson = "{}", ExpectJson = "{}" };
        caseRepo.Add(testCase);
        caseRepo.SaveChanges();
        caseRepo.GetByRule(rule.Id).Should().ContainSingle();
        testCase.InputJson = "{\"x\":1}";
        caseRepo.Update(testCase);
        caseRepo.SaveChanges();
        caseRepo.GetByRule(rule.Id).Single().InputJson.Should().Contain("x");

        var sug = new SuggestedTest { RuleId = rule.Id, PayloadJson = "{}", Score = 0.1, Hash = "h" };
        sugRepo.Add(sug);
        sugRepo.SaveChanges();
        sugRepo.Exists(rule.Id, "h").Should().BeTrue();
        sugRepo.GetByIds(rule.Id, new[] { sug.Id }).Should().HaveCount(1);
        sugRepo.GetByRule(rule.Id).Should().ContainSingle();
        sugRepo.Delete(sug.Id);
        sugRepo.SaveChanges();
        sugRepo.GetByRule(rule.Id).Should().BeEmpty();

        var link = new RuleTestCaseTag { RuleTestCaseId = testCase.Id, TestTagId = tag.Id };
        linkRepo.Add(link);
        linkRepo.SaveChanges();
        linkRepo.GetByTest(testCase.Id).Should().ContainSingle();
        linkRepo.Delete(testCase.Id, tag.Id);
        linkRepo.SaveChanges();
        linkRepo.GetByTest(testCase.Id).Should().BeEmpty();

        tagRepo.Delete(tag.Id);
        tagRepo.SaveChanges();
        tagRepo.GetAll().Should().BeEmpty();
    }
}

