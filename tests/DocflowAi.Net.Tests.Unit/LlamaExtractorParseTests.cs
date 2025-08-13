using System.Collections.Generic;
using DocflowAi.Net.Application.Profiles;
using DocflowAi.Net.Domain.Extraction;
using DocflowAi.Net.Infrastructure.Llm;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DocflowAi.Net.Tests.Unit;

public class LlamaExtractorParseTests
{
    [Fact]
    public void ParseResult_AllowsDuplicateKeys_LastWins()
    {
        var profile = new ExtractionProfile
        {
            Name = "tpl",
            DocumentType = "invoice",
            Language = "en",
            Fields = new List<FieldSpec>()
        };
        var raw = "{\"document_type\":\"invoice\",\"document_type\":\"receipt\",\"language\":\"en\",\"fields\":[]}";
        var logger = LoggerFactory.Create(b => { }).CreateLogger<LlamaExtractor>();

        var result = LlamaExtractor.ParseResult(raw, profile, profile.DocumentType, logger);

        Assert.Equal("receipt", result.DocumentType);
    }
}
