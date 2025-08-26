using System;
using DocflowAi.Net.Api.JobQueue.Abstractions;
using Hangfire;
using Hangfire.States;
using Hangfire.Storage;

namespace DocflowAi.Net.Api.JobQueue.Filters;

public class JobStateFilter : IApplyStateFilter
{
    public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        if (context.NewState is EnqueuedState &&
            context.BackgroundJob.Job.Args.Count > 0 &&
            context.BackgroundJob.Job.Args[0] is Guid jobId)
        {
            try
            {
                #pragma warning disable CS0618 // JobActivator.BeginScope is obsolete in Hangfire 1.x
                using var scope = JobActivator.Current.BeginScope();
                #pragma warning restore CS0618
                var repo = (IJobRepository)scope.Resolve(typeof(IJobRepository));
                var uow = (IUnitOfWork)scope.Resolve(typeof(IUnitOfWork));
                repo.UpdateStatus(jobId, "Queued");
                uow.SaveChanges();
            }
            catch (ObjectDisposedException)
            {
                // server shutting down, safe to ignore
            }
        }
    }

    public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction) { }
}
