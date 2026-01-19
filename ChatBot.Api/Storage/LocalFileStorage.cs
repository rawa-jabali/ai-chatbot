using ChatBot.Core.Interfaces;

namespace ChatBot.Api.Storage;

public class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IConfiguration config, IWebHostEnvironment env)
    {
        var configured = config["Storage:RootPath"];

        _root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(env.ContentRootPath, "storage")
            : configured;

        _root = ExpandTilde(_root);

        Directory.CreateDirectory(_root);

        Console.WriteLine($"Storage folder: {_root}");
    }

    public async Task<StoredFile> SaveAsync(string fileName, string contentType, Stream content, CancellationToken ct = default)
    {
        var safeName = MakeSafeFileName(fileName);
        var path = Path.Combine(_root, safeName);

        await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(fs, ct);

        return new StoredFile(
            FileName: safeName,
            FullPath: path,
            ContentType: string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
        );
    }

    public bool Exists(string fileName)
    {
        var safe = MakeSafeFileName(fileName);
        return File.Exists(Path.Combine(_root, safe));
    }

    public Task<Stream> OpenReadAsync(string fileName, CancellationToken ct = default)
    {
        var safe = MakeSafeFileName(fileName);
        var path = Path.Combine(_root, safe);
        Stream s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(s);
    }
    private static string ExpandTilde(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return path;
        if (path.StartsWith("~/") || path == "~")
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (path == "~") return home;
            return Path.Combine(home, path.Substring(2));
        }
        return path;
    }

    private static string MakeSafeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);

        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');

        // optional: منع أسماء فاضية
        if (string.IsNullOrWhiteSpace(name))
            name = $"file_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        return name;
    }
}