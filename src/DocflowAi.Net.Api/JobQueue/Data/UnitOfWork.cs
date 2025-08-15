using DocflowAi.Net.Api.JobQueue.Abstractions;

namespace DocflowAi.Net.Api.JobQueue.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly JobDbContext _db;

    public UnitOfWork(JobDbContext db)
    {
        _db = db;
    }

    public void SaveChanges() => _db.SaveChanges();
}
