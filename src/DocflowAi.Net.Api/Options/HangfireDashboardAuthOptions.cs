namespace DocflowAi.Net.Api.Options;

public class HangfireDashboardAuthOptions
{
    public const string SectionName = "HangfireDashboardAuth";
    public bool Enabled { get; set; } = false;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
