namespace DocflowAi.Net.Api.Models;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

public sealed class ProcessRequest
{
    [Required]
    public IFormFile File { get; init; } = default!;

    [Required]
    public string TemplateName { get; init; } = string.Empty;

    [Required]
    public string Prompt { get; init; } = string.Empty;

    [Required]
    public List<FieldRequest> Fields { get; init; } = [];
}
