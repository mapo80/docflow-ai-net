using System.Text.Json;
using BBoxEvalRunner.Models;

namespace BBoxEvalRunner;

public static class DatasetLoader
{
    public static IReadOnlyDictionary<string, List<string>> LoadManifest(DatasetConfig config)
    {
        if (config.Manifest == null || !File.Exists(config.Manifest))
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        var json = File.ReadAllText(config.Manifest);
        var entries = JsonSerializer.Deserialize<List<ManifestEntry>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new();
        return entries.ToDictionary(e => e.File, e => e.Fields, StringComparer.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<string> GetExpectedLabels(string fileName, DatasetConfig config)
    {
        var manifest = LoadManifest(config);
        if (manifest.TryGetValue(fileName, out var fields) && fields.Count > 0)
            return fields;
        return config.Fields;
    }
}
