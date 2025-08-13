using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using XFundEvalRunner.Models;

namespace XFundEvalRunner;

public static class XFundParser
{
    public static Dictionary<string, DocumentManifest> Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var result = new Dictionary<string, DocumentManifest>(StringComparer.OrdinalIgnoreCase);
        if (doc.RootElement.TryGetProperty("documents", out var documents))
        {
            foreach (var d in documents.EnumerateArray())
            {
                var img = d.GetProperty("img").GetString() ?? string.Empty;
                var nodes = d.GetProperty("document").EnumerateArray().ToList();
                var byId = nodes.ToDictionary(n => n.GetProperty("id").GetInt32());
                var fields = new List<FieldManifest>();
                foreach (var n in nodes)
                {
                    var label = n.GetProperty("label").GetString();
                    if (label != null && IsFieldLabel(label))
                    {
                        string fieldName = Normalize(GetText(n));
                        string expected = string.Empty;
                        var boxes = new List<int[]>();
                        bool linked = false;
                        if (n.TryGetProperty("linking", out var linking) && linking.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var pair in linking.EnumerateArray())
                            {
                                if (pair.ValueKind == JsonValueKind.Array && pair.GetArrayLength() == 2)
                                {
                                    int answerId = pair[1].GetInt32();
                                    if (byId.TryGetValue(answerId, out var ans))
                                    {
                                        linked = true;
                                        expected = string.IsNullOrEmpty(expected) ? GetText(ans) : expected + " " + GetText(ans);
                                        var box = GetBox(ans);
                                        if (box.Length == 4) boxes.Add(box);
                                    }
                                }
                            }
                        }
                        if (linked)
                        {
                            fields.Add(new FieldManifest
                            {
                                Name = fieldName,
                                ExpectedValue = Normalize(expected),
                                ExpectedBoxes = boxes
                            });
                        }
                    }
                }
                result[Path.GetFileName(img)] = new DocumentManifest { File = img, Fields = fields };
            }
        }
        return result;
    }

    private static bool IsFieldLabel(string label) =>
        label.Equals("question", StringComparison.OrdinalIgnoreCase) ||
        label.Equals("key", StringComparison.OrdinalIgnoreCase) ||
        label.Equals("header", StringComparison.OrdinalIgnoreCase);

    private static string GetText(JsonElement node)
    {
        if (node.TryGetProperty("text", out var t))
            return t.GetString() ?? string.Empty;
        return string.Empty;
    }

    private static int[] GetBox(JsonElement node)
    {
        if (node.TryGetProperty("box", out var b) && b.ValueKind == JsonValueKind.Array && b.GetArrayLength() == 4)
            return new[] { b[0].GetInt32(), b[1].GetInt32(), b[2].GetInt32(), b[3].GetInt32() };
        return Array.Empty<int>();
    }

    public static string Normalize(string text)
    {
        text = (text ?? string.Empty).Normalize(NormalizationForm.FormKC).ToLowerInvariant();
        text = Regex.Replace(text, "\\s+", " ").Trim();
        text = text.Replace("’", "'");
        text = text.Replace("\"", "\"");
        text = text.Replace("s.p.a.", "spa");
        return text;
    }
}
