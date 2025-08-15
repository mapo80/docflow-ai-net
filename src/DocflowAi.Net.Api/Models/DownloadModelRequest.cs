namespace DocflowAi.Net.Api.Models;

using System.ComponentModel.DataAnnotations;

public sealed class DownloadModelRequest
{
    [Required]
    public string HfKey { get; init; } = string.Empty;

    [Required]
    public string ModelRepo { get; init; } = string.Empty;

    [Required]
    public string ModelFile { get; init; } = string.Empty;
}
