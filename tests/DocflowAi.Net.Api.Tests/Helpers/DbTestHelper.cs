using Bogus;
using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.JobQueue.Models;

namespace DocflowAi.Net.Api.Tests.Helpers;

public static class DbTestHelper
{
    static DbTestHelper()
    {
        Randomizer.Seed = new Random(123);
    }

    public static void InsertJob(JobDbContext db, JobDocument doc)
    {
        db.Jobs.Add(doc);
        db.SaveChanges();
    }

    public static JobDocument? GetJob(JobDbContext db, Guid id) => db.Jobs.Find(id);

    public static void SeedJobs(JobDbContext db, IEnumerable<JobDocument> jobs)
    {
        db.Jobs.AddRange(jobs);
        db.SaveChanges();
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
            Model = "m",
            TemplateToken = "t",
            Paths = new JobDocument.PathInfo { Dir = "", Input = "", Output = "", Error = "" },
            Metrics = new JobDocument.MetricsInfo()
        };
    }
}
