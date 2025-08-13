using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace DocflowAi.Net.BBoxResolver;

/// <summary>Implementation of <see cref="IPointerResolver"/>.</summary>
public sealed class PointerResolver : IPointerResolver
{
    private readonly PointerOptions _options;
    private readonly PlainTextViewBuilder _textBuilder;
    private readonly ILogger<PointerResolver> _logger;
    private static readonly Regex WordIdRegex = new("^W(?<p>\\d+)_(?<w>\\d+)$", RegexOptions.Compiled);

    public PointerResolver(Microsoft.Extensions.Options.IOptions<PointerOptions> options, PlainTextViewBuilder textBuilder, ILogger<PointerResolver> logger)
    {
        _options = options.Value;
        _textBuilder = textBuilder;
        _logger = logger;
    }

    public bool TryResolve(DocumentIndex index, ExtractedField field, out BBoxResolveResult result)
    {
        result = new BBoxResolveResult(field.Key, field.Value, field.Confidence, Array.Empty<SpanEvidence>());
        var ptr = field.Pointer;
        if (ptr is null)
        {
            _logger.LogDebug("Field {FieldName} has no pointer", field.Key);
            return false;
        }

        var resolved = ptr.Mode switch
        {
            PointerMode.WordIds => ResolveWordIds(index, field, ptr.WordIds!, out result),
            PointerMode.Offsets => ResolveOffsets(index, field, ptr.Start ?? 0, ptr.End ?? 0, out result),
            _ => false
        };
        _logger.LogDebug("Pointer resolution for {FieldName} mode={Mode} success={Success}", field.Key, ptr.Mode, resolved);
        return resolved;
    }

    private bool ResolveWordIds(DocumentIndex index, ExtractedField field, string[] ids, out BBoxResolveResult result)
    {
        result = new BBoxResolveResult(field.Key, field.Value, field.Confidence, Array.Empty<SpanEvidence>());
        if (ids.Length == 0)
            return false;
        var pages = new List<int>();
        var words = new List<int>();
        foreach (var id in ids)
        {
            var m = WordIdRegex.Match(id);
            if (!m.Success) return false;
            var p = int.Parse(m.Groups["p"].Value);
            var w = int.Parse(m.Groups["w"].Value);
            if (p < 0 || p >= index.Pages.Length) return false;
            if (w < 0 || w >= index.Pages[p].Words.Count) return false;
            pages.Add(p); words.Add(w);
        }
        if (_options.Strict && pages.Distinct().Count() > 1)
            return false;
        var page = pages[0];
        words.Sort();
        var hasGap = false;
        for (int i = 1; i < words.Count; i++)
        {
            var gap = words[i] - words[i - 1] - 1;
            if (gap > 0) hasGap = true;
            if (_options.Strict && gap > _options.MaxGapBetweenIds)
                return false;
        }
        var wordObjs = words.Select(w => index.Pages[page].Words[w]).ToList();
        var bbox = Union(wordObjs.Select(w => w.BBox));
        var text = string.Join(" ", wordObjs.Select(w => w.Text));
        var confidence = _options.ConfidenceWhenStrict - (hasGap ? 0.05 : 0);
        var span = new SpanEvidence(page, words.ToArray(), bbox, text, 1.0, null);
        result = new BBoxResolveResult(field.Key, field.Value, confidence, new[] { span }, new Pointer(PointerMode.WordIds, ids, null, null));
        return true;
    }

    private bool ResolveOffsets(DocumentIndex index, ExtractedField field, int start, int end, out BBoxResolveResult result)
    {
        result = new BBoxResolveResult(field.Key, field.Value, field.Confidence, Array.Empty<SpanEvidence>());
        if (start >= end) return false;
        var (text, spans) = _textBuilder.Build(index);
        if (start < 0 || end > text.Length) return false;
        var matched = spans.Where(s => s.start >= start && s.start + s.len <= end).ToList();
        if (matched.Count == 0) return false;
        var pages = matched.Select(m => m.page).Distinct().ToList();
        if (_options.Strict && pages.Count > 1) return false;
        var page = matched[0].page;
        var wordIdx = matched.Select(m => m.wordIdx).OrderBy(i => i).ToList();
        var hasGap = false;
        for (int i = 1; i < wordIdx.Count; i++)
        {
            var gap = wordIdx[i] - wordIdx[i - 1] - 1;
            if (gap > 0) hasGap = true;
            if (_options.Strict && gap > _options.MaxGapBetweenIds) return false;
        }
        var wordObjs = wordIdx.Select(w => index.Pages[page].Words[w]).ToList();
        var bbox = Union(wordObjs.Select(w => w.BBox));
        var textValue = string.Join(" ", wordObjs.Select(w => w.Text));
        var confidence = _options.ConfidenceWhenStrict - (hasGap ? 0.05 : 0);
        var span = new SpanEvidence(page, wordIdx.ToArray(), bbox, textValue, 1.0, null);
        result = new BBoxResolveResult(field.Key, field.Value, confidence, new[] { span }, new Pointer(PointerMode.Offsets, null, start, end));
        return true;
    }

    private static Box Union(IEnumerable<Box> boxes)
    {
        var minX = float.MaxValue; var minY = float.MaxValue;
        var maxX = float.MinValue; var maxY = float.MinValue;
        foreach (var b in boxes)
        {
            minX = Math.Min(minX, b.X);
            minY = Math.Min(minY, b.Y);
            maxX = Math.Max(maxX, b.X + b.W);
            maxY = Math.Max(maxY, b.Y + b.H);
        }
        return new Box(minX, minY, maxX - minX, maxY - minY);
    }
}

