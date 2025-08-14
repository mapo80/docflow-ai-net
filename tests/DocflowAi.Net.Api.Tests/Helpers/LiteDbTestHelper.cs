using Bogus;
using DocflowAi.Net.Api.JobQueue.Models;
using LiteDB;

namespace DocflowAi.Net.Api.Tests.Helpers;

public static class LiteDbTestHelper
{
    static LiteDbTestHelper()
    {
        Randomizer.Seed = new Random(123);
    }

    public static LiteDatabase Open(string path) => new LiteDatabase(path);

    public static void InsertJob(string path, JobDocument doc)
    {
        using var db = Open(path);
        var col = db.GetCollection<JobDocument>("jobs");
        col.Insert(doc);
    }

    public static JobDocument? GetJob(string path, Guid id)
    {
        using var db = Open(path);
        return db.GetCollection<JobDocument>("jobs").FindById(id);
    }

    public static void SeedJobs(LiteDatabase db, IEnumerable<JobDocument> jobs)
    {
        var col = db.GetCollection<JobDocument>("jobs");
        col.InsertBulk(jobs);
    }

    public static JobDocument CreateJob(Guid id, string status, DateTimeOffset createdAt, int progress = 0)
    {
        var faker = new Faker();
        return new JobDocument
        {
            Id = id,
            Status = status,
            Progress = progress,
            Attempts = 0,
            Priority = 0,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            Hash = faker.Random.Hash(),
            Paths = new JobDocument.PathInfo { Dir = "", Input = "", Prompt = null, Fields = null, Output = "", Error = "" },
            Metrics = new JobDocument.MetricsInfo()
        };
    }
}
