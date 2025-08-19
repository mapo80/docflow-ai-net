namespace DocflowAi.Net.Api.Options;

    public class JobQueueOptions
    {
        public const string SectionName = "JobQueue";
        public string DataRoot { get; set; } = "./data/jobs";
        public DatabaseOptions Database { get; set; } = new();
        public QueueOptions Queue { get; set; } = new();
        public RateLimitOptions RateLimit { get; set; } = new();
        public ConcurrencyOptions Concurrency { get; set; } = new();
        public TimeoutOptions Timeouts { get; set; } = new();
        public UploadOptions UploadLimits { get; set; } = new();
        public ImmediateOptions Immediate { get; set; } = new();
        public CleanupOptions Cleanup { get; set; } = new();
        public int JobTTLDays { get; set; } = 14;
        public bool EnableHangfireDashboard { get; set; } = true;
        public bool SeedDefaults { get; set; } = true;

    public class DatabaseOptions
    {
        public string Provider { get; set; } = "sqlite";
        public string ConnectionString { get; set; } = string.Empty;
    }

    public class QueueOptions
    {
        public int MaxQueueLength { get; set; } = 100;
        public int LeaseWindowSeconds { get; set; } = 120;
        public int MaxAttempts { get; set; } = 5;
    }

    public class RateLimitOptions
    {
        public PolicyOptions General { get; set; } = new();
        public PolicyOptions Submit { get; set; } = new();

        public class PolicyOptions
        {
            public int PermitPerWindow { get; set; }
            public int WindowSeconds { get; set; }
            public int QueueLimit { get; set; }
        }
    }

    public class ConcurrencyOptions
    {
        public int MaxParallelHeavyJobs { get; set; }
        public int HangfireWorkerCount { get; set; }
    }

    public class TimeoutOptions
    {
        public int JobTimeoutSeconds { get; set; } = 900;
    }

    public class UploadOptions
    {
        public int MaxRequestBodyMB { get; set; } = 20;
    }

    public class ImmediateOptions
    {
        public bool Enabled { get; set; }
        public int MaxParallel { get; set; } = 1;
        public int TimeoutSeconds { get; set; } = 30;
        public bool FallbackToQueue { get; set; } = true;
    }

    public class CleanupOptions
    {
        public bool Enabled { get; set; } = true;
        public int DailyHour { get; set; } = 3;
        public int DailyMinute { get; set; } = 15;
    }
}
