using System.IO.Compression;
using System.Text.RegularExpressions;

namespace WelfareSystem.Api.Services;

// Private files are only streamed through an authorized controller.
public sealed partial class PrivateDocumentStore(IHostEnvironment environment, IConfiguration configuration, IFileScanner? scanner = null)
{
    public const long MaximumSize = 10 * 1024 * 1024;
    private string Root => Path.GetFullPath(configuration["Documents:StoragePath"] ?? Path.Combine(environment.ContentRootPath, "App_Data", "Documents"));
    [GeneratedRegex(@"^[a-f0-9]{32}\.(pdf|png|jpg|jpeg|doc|docx)$")]
    private static partial Regex StorageKey();
    public string? Resolve(string key) => StorageKey().IsMatch(key) ? Path.Combine(Root, key) : null;
    public bool Exists(string key) => Resolve(key) is string path && File.Exists(path);
    public void Remove(string key) { if (Resolve(key) is string path) File.Delete(path); }
    public Stream? Open(string key) => Resolve(key) is string path && File.Exists(path) ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read) : null;

    public async Task<(string Key, string ContentType, long Size, string Sha256, string ScanStatus)> Save(IFormFile file, CancellationToken ct)
    {
        if (file.Length is <= 0 or > MaximumSize) throw new InvalidDataException("Choose a non-empty file no larger than 10 MB.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        using var buffer = new MemoryStream();
        var block = new byte[81920];
        await using (var source = file.OpenReadStream()) {
            int read;
            while ((read = await source.ReadAsync(block, ct)) != 0) {
                if (buffer.Length + read > MaximumSize) throw new InvalidDataException("File exceeds 10 MB.");
                await buffer.WriteAsync(block.AsMemory(0, read), ct);
            }
        }
        var bytes = buffer.ToArray();
        var mime = ValidateContent(extension, bytes);
        if (scanner != null) await scanner.Check(bytes, ct);
        else if (!environment.IsDevelopment()) throw new IOException("Configure file scanning before accepting production uploads.");
        var key = Guid.NewGuid().ToString("N") + extension;
        Directory.CreateDirectory(Root);
        try { await File.WriteAllBytesAsync(Resolve(key)!, bytes, ct); }
        catch { Remove(key); throw; }
        return (key, mime, bytes.LongLength, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)), scanner?.Mode ?? "Unscanned development");
    }

    public static string ValidateContent(string extension, byte[] bytes)
    {
        bool Starts(params byte[] signature) => bytes.AsSpan().StartsWith(signature);
        if (extension == ".pdf" && Starts(37, 80, 68, 70, 45)) return "application/pdf";
        if (extension == ".png" && Starts(137, 80, 78, 71, 13, 10, 26, 10)) return "image/png";
        if (extension is ".jpg" or ".jpeg" && Starts(255, 216, 255)) return "image/jpeg";
        if (extension == ".doc" && Starts(208, 207, 17, 224, 161, 177, 26, 225)) return "application/msword";
        if (extension == ".docx" && Starts(80, 75, 3, 4)) {
            try {
                using var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
                long expanded = 0;
                if (zip.Entries.Count > 1000) throw new InvalidDataException();
                foreach (var entry in zip.Entries) {
                    if (entry.Length > 50 * 1024 * 1024 - expanded || entry.FullName.Contains("vbaProject", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException();
                    expanded += entry.Length;
                }
                if (zip.GetEntry("[Content_Types].xml") != null && zip.GetEntry("word/document.xml") != null)
                    return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            } catch (InvalidDataException) { }
        }
        throw new InvalidDataException("File contents do not match a supported PDF, Word, PNG, or JPEG file. Macro-enabled DOCX files are not accepted.");
    }
}
