namespace DocflowAi.Net.Api.Models;

using System.ComponentModel.DataAnnotations;

public sealed record FieldRequest(
    [property: Required] string FieldName,
    string? Format);
