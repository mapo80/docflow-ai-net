using System;
using System.Reflection;
using System.IO;
using DocflowAi.Net.Api.JobQueue.Endpoints;
using FluentAssertions;
using Xunit;

namespace DocflowAi.Net.Api.Tests;

public class JobEndpointsHelpersTests
{
    private static T Invoke<T>(string method, params object?[] args)
    {
        var mi = typeof(JobEndpoints).GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!;
        return (T)mi.Invoke(null, args)!;
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    public void ToPublicPath_Transforms_Correctly(string? input, string? expected)
    {
        var id = Guid.Empty;
        Invoke<string?>("ToPublicPath", id, input).Should().Be(expected);
    }

    [Fact]
    public void ToPublicPath_Transforms_Existing_File()
    {
        var id = Guid.Empty;
        var tmp = Path.GetTempFileName();
        try
        {
            Invoke<string?>("ToPublicPath", id, tmp)
                .Should()
                .Be($"/api/v1/jobs/{id}/files/{Path.GetFileName(tmp)}");
        }
        finally
        {
            File.Delete(tmp);
        }
    }

    [Theory]
    [InlineData(".json", "application/json")]
    [InlineData(".txt", "text/plain")]
    [InlineData(".bin", "application/octet-stream")]
    public void GetContentType_Maps_Extensions(string suffix, string expected)
    {
        var name = "file" + suffix;
        Invoke<string>("GetContentType", name).Should().Be(expected);
    }

    [Theory]
    [InlineData("Queued", "Pending")]
    [InlineData("Running", "Processing")]
    [InlineData("Succeeded", "Completed")]
    [InlineData("Failed", "Failed")]
    [InlineData("Other", "Other")]
    public void MapDerivedStatus_Maps_Status(string input, string expected)
    {
        Invoke<string>("MapDerivedStatus", input).Should().Be(expected);
    }
}
