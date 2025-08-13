using XFundEvalRunner;
using Xunit;

public class ManifestTests
{
    [Fact]
    public void Manifest_From_XFUND_Annotations()
    {
        string json = @"{ ""documents"": [ { ""img"": ""doc1.jpg"", ""document"": [ {""id"":1,""text"":""Name"",""label"":""question"",""linking"": [[1,2]]},{""id"":2,""text"":""Alice"",""label"":""answer"",""box"": [0,0,1,1],""linking"": [[1,2]]} ] } ] }";
        var dict = XFundParser.Parse(json);
        Assert.True(dict.TryGetValue("doc1.jpg", out var manifest));
        var field = Assert.Single(manifest.Fields);
        Assert.Equal("name", field.Name);
        Assert.Equal("alice", field.ExpectedValue);
        Assert.NotEmpty(field.ExpectedBoxes);
    }
}
