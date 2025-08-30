using System.Text;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using DocflowAi.Net.Api.Rules.Abstractions;
using DocflowAi.Net.Api.Rules.Models;
using DocflowAi.Net.Api.Rules.Runtime;
using Microsoft.Extensions.Logging;

namespace DocflowAi.Net.Api.Rules.Services;

/// <summary>
/// Provides CRUD and execution helpers for rule functions while delegating all data access to repositories.
/// </summary>
public class RuleService
{
    private readonly IRuleFunctionRepository _rules;
    private readonly IRuleTestCaseRepository _tests;
    private readonly IRuleEngine _engine;
    private readonly IScriptRunner _runner;
    private readonly ILogger<RuleService> _log;

    public RuleService(IRuleFunctionRepository rules, IRuleTestCaseRepository tests, IRuleEngine engine, IScriptRunner runner, ILogger<RuleService> log)
    { _rules = rules; _tests = tests; _engine = engine; _runner = runner; _log = log; }

    public Task<(int total, List<RuleFunction> items)> GetAllAsync(string? search, string? sortBy, string? sortDir, int page, int pageSize, CancellationToken ct)
    {
        var items = _rules.GetAll().ToList();
        if (!string.IsNullOrWhiteSpace(search))
            items = items.Where(x => x.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || (x.Description ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        items = (sortBy, sortDir?.ToLowerInvariant()) switch
        {
            ("name", "desc") => items.OrderByDescending(x => x.Name).ToList(),
            ("updatedAt", "asc") => items.OrderBy(x => x.UpdatedAt).ToList(),
            ("updatedAt", "desc") => items.OrderByDescending(x => x.UpdatedAt).ToList(),
            _ => items.OrderBy(x => x.Name).ToList(),
        };
        var total = items.Count;
        var pageItems = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult((total, pageItems));
    }

    public Task<RuleFunction?> GetAsync(Guid id, CancellationToken ct)
        => Task.FromResult(_rules.GetById(id));

    public Task<(RuleFunction? rule, bool conflict)> CreateAsync(string name, string? description, string code, string? readsCsv, string? writesCsv, bool enabled, CancellationToken ct)
    {
        if (_rules.GetByName(name) != null)
            return Task.FromResult<(RuleFunction?, bool)>((null, true));
        var rule = new RuleFunction
        {
            Name = name,
            Description = description,
            Code = code,
            CodeHash = Hash(code),
            ReadsCsv = readsCsv,
            WritesCsv = writesCsv,
            Enabled = enabled,
            IsBuiltin = false
        };
        _rules.Add(rule);
        _rules.SaveChanges();
        _log.LogInformation("Rule created {Name}", name);
        return Task.FromResult<(RuleFunction?, bool)>((rule, false));
    }

    public enum UpdateResult { NotFound, Builtin, Ok }

    public Task<UpdateResult> UpdateAsync(Guid id, string name, string? description, string code, string? readsCsv, string? writesCsv, bool enabled, CancellationToken ct)
    {
        var r = _rules.GetById(id);
        if (r == null) return Task.FromResult(UpdateResult.NotFound);
        if (r.IsBuiltin) return Task.FromResult(UpdateResult.Builtin);
        r.Name = name;
        r.Description = description;
        r.Code = code;
        r.CodeHash = Hash(code);
        r.ReadsCsv = readsCsv;
        r.WritesCsv = writesCsv;
        r.Enabled = enabled;
        _rules.Update(r);
        _rules.SaveChanges();
        _log.LogInformation("Rule updated {Id}", id);
        return Task.FromResult(UpdateResult.Ok);
    }

    public Task<bool> StageAsync(Guid id, CancellationToken ct)
    {
        var r = _rules.GetById(id);
        if (r == null) return Task.FromResult(false);
        r.Status = RuleStatus.Staged;
        _rules.Update(r);
        _rules.SaveChanges();
        _log.LogInformation("Rule staged {Id}", id);
        return Task.FromResult(true);
    }

    public Task<bool> PublishAsync(Guid id, CancellationToken ct)
    {
        var r = _rules.GetById(id);
        if (r == null) return Task.FromResult(false);
        r.Status = RuleStatus.Published;
        _rules.Update(r);
        _rules.SaveChanges();
        _log.LogInformation("Rule published {Id}", id);
        return Task.FromResult(true);
    }

    public async Task<(bool ok, string[] errors)?> CompileAsync(Guid id, CancellationToken ct)
    {
        var r = _rules.GetById(id);
        if (r == null) return null;
        return await _runner.CompileAsync(r.Code, ct);
    }

    public async Task<RunResult?> RunAsync(Guid id, JsonObject input, CancellationToken ct)
    {
        var r = _rules.GetById(id);
        if (r == null) return null;
        return await _engine.RunAsync(r.Code, input, ct);
    }

    public Task<RuleFunction?> CloneAsync(Guid id, string? newName, bool includeTests, CancellationToken ct)
    {
        var src = _rules.GetById(id);
        if (src == null) return Task.FromResult<RuleFunction?>(null);
        var name = string.IsNullOrWhiteSpace(newName) ? src.Name + " (copy)" : newName!;
        var dup = new RuleFunction
        {
            Name = name,
            Version = src.Version,
            IsBuiltin = false,
            Enabled = src.Enabled,
            Description = src.Description,
            Code = src.Code,
            CodeHash = Hash(src.Code),
            ReadsCsv = src.ReadsCsv,
            WritesCsv = src.WritesCsv
        };
        _rules.Add(dup);
        _rules.SaveChanges();
        if (includeTests)
        {
            foreach (var t in _tests.GetByRule(id))
            {
                _tests.Add(new RuleTestCase
                {
                    RuleFunctionId = dup.Id,
                    Name = t.Name,
                    InputJson = t.InputJson,
                    ExpectJson = t.ExpectJson,
                    Suite = t.Suite,
                    TagsCsv = t.TagsCsv,
                    Priority = t.Priority,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }
            _tests.SaveChanges();
        }
        _log.LogInformation("Rule cloned {Src} -> {Dest}", id, dup.Id);
        return Task.FromResult<RuleFunction?>(dup);
    }

    private static string Hash(string code)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
}

