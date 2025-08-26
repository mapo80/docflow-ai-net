using System.Collections.Generic;
using System.Reflection;
using SkiaSharp;
using Xunit;

namespace DocflowAi.Net.Api.Tests;

public class RapidOcrPatchesTests
{
    [Fact]
    public void GetPartImages_null_boxes_returns_empty()
    {
        using var bmp = new SKBitmap(10, 10);
        var utilsType = typeof(RapidOcrNet.RapidOcr).Assembly.GetType("RapidOcrNet.OcrUtils")!;
        var method = utilsType.GetMethod("GetPartImages", BindingFlags.Public | BindingFlags.Static)!;
        var result = (IEnumerable<SKBitmap>)method.Invoke(null, new object?[] { bmp, null })!;
        Assert.Empty(result);
    }
}
