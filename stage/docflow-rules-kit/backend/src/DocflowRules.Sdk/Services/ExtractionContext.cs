using System.Text.Json.Nodes;

namespace DocflowRules.Sdk;

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
    public object? Get(string key) => _fields.TryGetValue(key, out var f) ? f.Value : null;
    public T? Get<T>(string key)
    {
        if (_fields.TryGetValue(key, out var f))
        {
            if (f.Value is null) return default;
            if (f.Value is T t) return t;
            try { return (T)Convert.ChangeType(f.Value, typeof(T)); }
            catch { return default; }
        }
        return default;
    }

    public void Upsert(string key, object? value, double confidence = 0.99, string source = "rule")
        => _fields[key] = new FieldValue(value, confidence, source);

    public JsonObject ToJson() => new(_fields.ToDictionary(kv => kv.Key, kv => JsonValue.Create(kv.Value.Value)));

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
                diffs.Add(new JsonObject { ["field"] = k, ["before"] = b, ["after"] = a });
        }
        return diffs;
    }
}

public readonly record struct FieldValue(object? Value, double Confidence, string Source);
