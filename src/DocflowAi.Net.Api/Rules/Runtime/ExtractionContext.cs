using System.Text.Json;
using System.Text.Json.Nodes;

namespace DocflowAi.Net.Api.Rules.Runtime;

public sealed class ExtractionContext
{
    private readonly Dictionary<string, FieldValue> _fields;
    public IReadOnlyDictionary<string, FieldValue> Fields => _fields;
    public JsonObject Meta { get; }

    public ExtractionContext(Dictionary<string, FieldValue>? fields = null, JsonObject? meta = null)
    {
        _fields = fields ?? new(StringComparer.OrdinalIgnoreCase);
        Meta = meta ?? new JsonObject();
    }

    public bool Has(string key) => _fields.ContainsKey(key);

    public object? Get(string key)
    {
        if (!_fields.TryGetValue(key, out var f)) return null;
        var v = f.Value;
        if (v is JsonElement je)
        {
            switch (je.ValueKind)
            {
                case JsonValueKind.String:
                    return je.GetString();
                case JsonValueKind.Number:
                    if (je.TryGetInt64(out var l)) return l;
                    if (je.TryGetDouble(out var d)) return d;
                    break;
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null;
            }
            return je.ToString();
        }
        return v;
    }

    public T? Get<T>(string key)
    {
        var val = Get(key);
        if (val is null) return default;
        if (val is T t) return t;
        try { return (T)Convert.ChangeType(val, typeof(T)); }
        catch { return default(T?); }
    }

    public void Upsert(string key, object? value, double confidence = 0.99, string source = "rule")
        => _fields[key] = new FieldValue(value, confidence, source);

    public JsonObject ToJson()
    {
        var obj = new JsonObject();
        foreach (var (k, v) in _fields)
        {
            if (v.Value is JsonNode n)
                obj[k] = n.DeepClone();
            else
                obj[k] = JsonValue.Create(v.Value);
        }
        return obj;
    }

    public JsonArray DiffSince(JsonObject before)
    {
        var after = ToJson();
        var diffs = new JsonArray();

        var beforeKeys = before.Select(kvp => kvp.Key).ToHashSet();
        var afterKeys = after.Select(kvp => kvp.Key).ToHashSet();
        foreach (var k in beforeKeys.Union(afterKeys))
        {
            before.TryGetPropertyValue(k, out var b);
            after.TryGetPropertyValue(k, out var a);
            if ((b?.ToJsonString() ?? "null") != (a?.ToJsonString() ?? "null"))
                diffs.Add(new JsonObject { ["field"] = k, ["before"] = b?.DeepClone(), ["after"] = a?.DeepClone() });
        }
        return diffs;
    }
}

public readonly record struct FieldValue(object? Value, double Confidence, string Source);

