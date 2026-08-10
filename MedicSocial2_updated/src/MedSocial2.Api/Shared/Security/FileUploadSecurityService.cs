using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;

namespace Shared.Security;

public sealed class FileSecurityOptions
{
    public long DefaultMaxFileSizeBytes { get; set; } = 50_000_000;
    public string[] BlockedExtensions { get; set; } = [".exe", ".dll", ".com", ".bat", ".cmd", ".ps1", ".sh", ".msi", ".scr"];
    public string? ClamAvHost { get; set; }
    public int ClamAvPort { get; set; } = 3310;
    public bool RequireClamAv { get; set; }
}

public record FileSecurityResult(bool IsSafe, string? Error)
{
    public static FileSecurityResult Safe() => new(true, null);
    public static FileSecurityResult Unsafe(string error) => new(false, error);
}

public interface IFileUploadSecurityService
{
    Task<FileSecurityResult> ValidateAsync(IFormFile file, long? maxFileSizeBytes = null, CancellationToken cancellationToken = default);
}

public sealed class FileUploadSecurityService : IFileUploadSecurityService
{
    private static readonly byte[] EicarMarker = Encoding.ASCII.GetBytes("EICAR-STANDARD-ANTIVIRUS-TEST-FILE");
    private readonly FileSecurityOptions _options;

    public FileUploadSecurityService(IOptions<FileSecurityOptions> options) => _options = options.Value;

    public async Task<FileSecurityResult> ValidateAsync(IFormFile file, long? maxFileSizeBytes = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (file.Length <= 0) return FileSecurityResult.Unsafe("The uploaded file is empty.");
            var maxBytes = maxFileSizeBytes ?? _options.DefaultMaxFileSizeBytes;
            if (maxBytes > 0 && file.Length > maxBytes) return FileSecurityResult.Unsafe($"The uploaded file exceeds the {Math.Round(maxBytes / 1024d / 1024d, 1)} MB limit.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (_options.BlockedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return FileSecurityResult.Unsafe("Executable and script files are not accepted.");
            }

            await using var memory = new MemoryStream();
            await file.CopyToAsync(memory, cancellationToken);
            var bytes = memory.ToArray();
            if (bytes.Length >= 2 && bytes[0] == (byte)'M' && bytes[1] == (byte)'Z')
            {
                return FileSecurityResult.Unsafe("Executable content was detected in the uploaded file.");
            }
            if (bytes.AsSpan().IndexOf(EicarMarker) >= 0)
            {
                return FileSecurityResult.Unsafe("Malware test content was detected in the uploaded file.");
            }

            if (string.IsNullOrWhiteSpace(_options.ClamAvHost))
            {
                return _options.RequireClamAv
                    ? FileSecurityResult.Unsafe("Malware scanning is required but the scanner is unavailable.")
                    : FileSecurityResult.Safe();
            }

            return await ScanWithClamAvAsync(bytes, cancellationToken);
        }
        catch (Exception ex)
        {
            return FileSecurityResult.Unsafe($"File security validation failed: {ex.Message}");
        }
    }

    private async Task<FileSecurityResult> ScanWithClamAvAsync(byte[] bytes, CancellationToken cancellationToken)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(_options.ClamAvHost!, _options.ClamAvPort, cancellationToken);
        await using var stream = client.GetStream();
        await stream.WriteAsync("zINSTREAM\0"u8.ToArray(), cancellationToken);

        const int chunkSize = 8192;
        for (var offset = 0; offset < bytes.Length; offset += chunkSize)
        {
            var length = Math.Min(chunkSize, bytes.Length - offset);
            var lengthBytes = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(length));
            await stream.WriteAsync(lengthBytes, cancellationToken);
            await stream.WriteAsync(bytes.AsMemory(offset, length), cancellationToken);
        }
        await stream.WriteAsync(new byte[4], cancellationToken);
        await stream.FlushAsync(cancellationToken);

        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var response = await reader.ReadToEndAsync(cancellationToken);
        if (response.Contains("FOUND", StringComparison.OrdinalIgnoreCase))
        {
            return FileSecurityResult.Unsafe("The uploaded file failed malware scanning.");
        }
        return response.Contains("OK", StringComparison.OrdinalIgnoreCase)
            ? FileSecurityResult.Safe()
            : FileSecurityResult.Unsafe("The malware scanner returned an unknown result.");
    }
}
