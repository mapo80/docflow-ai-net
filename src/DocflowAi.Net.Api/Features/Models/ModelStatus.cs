namespace DocflowAi.Net.Api.Features.Models;

public enum ModelStatus
{
    NotDownloaded = 0,
    Downloading = 1,
    Available = 2,
    Error = 3,
    Deleting = 4
}
