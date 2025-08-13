using System.IO.Compression;
using System.Text.Json;
using XFundEvalRunner.Models;

namespace XFundEvalRunner;

public static class XFundDataset
{
    public static IReadOnlyList<string> SelectSubset(IEnumerable<string> files, DatasetConfig config)
    {
        return files.OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                    .Take(config.MaxFiles)
                    .ToList();
    }

    public static async Task<IReadOnlyList<DocumentManifest>> LoadAsync(DatasetConfig config)
    {
        if (!File.Exists(config.ZipPath))
            await DownloadFileAsync(config.DownloadUrl, config.ZipPath);
        if (!Directory.Exists(config.ExtractPath))
            ZipFile.ExtractToDirectory(config.ZipPath, config.ExtractPath);

        if (!File.Exists(config.AnnotationPath))
            await DownloadFileAsync(config.AnnotationUrl, config.AnnotationPath);

        Dictionary<string, DocumentManifest> manifests = new(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(config.AnnotationPath))
        {
            var json = await File.ReadAllTextAsync(config.AnnotationPath);
            manifests = XFundParser.Parse(json);
        }

        var images = Directory.EnumerateFiles(config.ExtractPath)
                              .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                              .ToList();
        var subset = SelectSubset(images, config);
        var result = new List<DocumentManifest>();
        foreach (var img in subset)
        {
            var name = Path.GetFileName(img);
            if (manifests.TryGetValue(name, out var manifest))
            {
                manifest.File = img;
                result.Add(manifest);
            }
            else
            {
                result.Add(new DocumentManifest { File = img, Fields = new List<FieldManifest>() });
            }
        }
        return result;
    }

    private static async Task DownloadFileAsync(string url, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var http = new HttpClient();
        var data = await http.GetByteArrayAsync(url);
        await File.WriteAllBytesAsync(path, data);
    }
}
