namespace ChatBot.Core.Interfaces;

public record StoredFile(string FileName, string FullPath, string ContentType);

public interface IFileStorage
{
    Task<StoredFile> SaveAsync(string fileName, string contentType, Stream content, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string fileName, CancellationToken ct = default);
    bool Exists(string fileName);
}