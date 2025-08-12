using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace DocflowAi.Net.Tests.Unit;

public class DatasetTests
{
    private static readonly string DatasetPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "dataset");

    [Fact]
    public void DatasetFilesExist()
    {
        File.Exists(Path.Combine(DatasetPath, "prompt.txt")).Should().BeTrue();
        File.Exists(Path.Combine(DatasetPath, "sample_invoice.pdf")).Should().BeTrue();
        File.Exists(Path.Combine(DatasetPath, "sample_invoice.png")).Should().BeTrue();
        File.Exists(Path.Combine(DatasetPath, "valori_campi.txt")).Should().BeTrue();
    }

    [Fact]
    public void PromptContainsExpectedFields()
    {
        var text = File.ReadAllText(Path.Combine(DatasetPath, "prompt.txt"));
        text.Should().Contain("company_name");
        text.Should().Contain("document_type");
        text.Should().Contain("invoice_number");
        text.Should().Contain("invoice_date");
    }
}
