using Bogus;
using DocflowAi.Net.Api.JobQueue.Data;
using DocflowAi.Net.Api.JobQueue.Models;
using DocflowAi.Net.Application.Markdown;

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
            Language = "eng",
            Engine = OcrEngine.Tesseract,
            Paths = new JobDocument.PathInfo
            {
                Dir = string.Empty,
                Input = new JobDocument.DocumentInfo { Path = string.Empty },
                Prompt = new JobDocument.DocumentInfo { Path = string.Empty },
                Output = new JobDocument.DocumentInfo { Path = string.Empty },
                Error = new JobDocument.DocumentInfo { Path = string.Empty },
                Markdown = new JobDocument.DocumentInfo { Path = string.Empty }
            },
            Metrics = new JobDocument.MetricsInfo()
        };
    }
}
