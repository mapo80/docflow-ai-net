using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.Model.Repositories;
using DocflowAi.Net.Api.Model.Services;
using DocflowAi.Net.Api.Options;
using DocflowAi.Net.Application.Abstractions;
using DocflowAi.Net.Infrastructure.Security;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using System.Linq.Expressions;
using System.IO;
using System;
using System.Linq;

namespace DocflowAi.Net.Api.Tests;

public class ModelServiceTests
{
    private class FakeBackgroundJobClient : IBackgroundJobClient
    {
        public string? Enqueued { get; private set; }
        public string Enqueue<T>(Expression<Func<T, Task>> methodCall)
        {
            Enqueued = Guid.NewGuid().ToString();
            return Enqueued;
        }
        public string Enqueue<T>(Expression<Action<T>> methodCall)
        {
            Enqueued = Guid.NewGuid().ToString();
            return Enqueued;
        }
        public string Enqueue(Expression<Action> methodCall) => Enqueue<object>(_ => Task.CompletedTask);
        public string Enqueue(Expression<Func<Task>> methodCall) => Enqueue<object>(_ => Task.CompletedTask);
        public bool ChangeState(string jobId, IState state, string fromState) => true;
        public string ContinueJobWith(string jobId, Expression<Action> methodCall) => jobId;
        public string ContinueJobWith<T>(string jobId, Expression<Action<T>> methodCall) => jobId;
        public string ContinueJobWith(string jobId, Expression<Func<Task>> methodCall) => jobId;
        public string ContinueJobWith<T>(string jobId, Expression<Func<T, Task>> methodCall) => jobId;
        public string Create(Job job, IState state)
        {
            Enqueued = Guid.NewGuid().ToString();
            return Enqueued;
        }
        private class Dummy : IDisposable { public void Dispose() { } }
        public string Schedule(Expression<Action> methodCall, TimeSpan delay) => Guid.NewGuid().ToString();
        public string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay) => Guid.NewGuid().ToString();
        public string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay) => Guid.NewGuid().ToString();
        public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay) => Guid.NewGuid().ToString();
    }

    private static IModelService CreateService(JobDbContext db, string dir, FakeBackgroundJobClient client)
    {
        var config = new ConfigurationBuilder().Build();
        var protector = new SecretProtector(config);
        var repo = new ModelRepository(db, protector);
        var logger = new LoggerFactory().CreateLogger<ModelService>();
        var opts = Microsoft.Extensions.Options.Options.Create(new ModelDownloadOptions { LogDirectory = dir, ModelDirectory = dir });
        return new ModelService(repo, logger, client, opts);
    }

    [Fact]
    public void CreateModel_StoresEncryptedSecrets()
    {
        var options = new DbContextOptionsBuilder<JobDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new JobDbContext(options);
        var client = new FakeBackgroundJobClient();
        var service = CreateService(db, Path.GetTempPath(), client);
        var dto = service.Create(new CreateModelRequest
        {
            Name = "test",
            Type = "hosted-llm",
            Provider = "openai",
            BaseUrl = "https://api",
            ApiKey = "plain-secret"
        });
        dto.HasApiKey.Should().BeTrue();
        db.Models.First().ApiKeyEncrypted.Should().NotBe("plain-secret");
        dto.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        dto.UpdatedAt.Should().BeCloseTo(dto.CreatedAt, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CreateModel_DuplicateName_Throws()
    {
        var options = new DbContextOptionsBuilder<JobDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new JobDbContext(options);
        var client = new FakeBackgroundJobClient();
        var service = CreateService(db, Path.GetTempPath(), client);
        service.Create(new CreateModelRequest { Name = "test", Type = "local", HfRepo = "r", ModelFile = "f" });
        Action act = () => service.Create(new CreateModelRequest { Name = "test", Type = "local", HfRepo = "r2", ModelFile = "f2" });
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void StartDownload_SetsLogPath()
    {
        var options = new DbContextOptionsBuilder<JobDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new JobDbContext(options);
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        var client = new FakeBackgroundJobClient();
        var service = CreateService(db, dir, client);
        var model = service.Create(new CreateModelRequest { Name = "m1", Type = "local", HfRepo = "r", ModelFile = "f" });
        service.StartDownload(model.Id);
        var entity = db.Models.Find(model.Id);
        entity!.DownloadLogPath!.Should().Contain(model.Id.ToString());
        client.Enqueued.Should().NotBeNull();
    }
}
