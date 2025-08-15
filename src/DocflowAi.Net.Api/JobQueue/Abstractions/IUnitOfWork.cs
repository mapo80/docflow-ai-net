namespace DocflowAi.Net.Api.JobQueue.Abstractions;

public interface IUnitOfWork
{
    void SaveChanges();
}
