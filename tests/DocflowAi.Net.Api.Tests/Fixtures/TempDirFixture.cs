namespace DocflowAi.Net.Api.Tests.Fixtures;

public class TempDirFixture : IDisposable
{
    public string RootPath { get; }

    public TempDirFixture()
    {
        RootPath = Path.Combine(Directory.GetCurrentDirectory(), ".testrun", Guid.NewGuid().ToString());
        Directory.CreateDirectory(RootPath);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(RootPath))
                Directory.Delete(RootPath, true);
        }
        catch { }
    }
}
