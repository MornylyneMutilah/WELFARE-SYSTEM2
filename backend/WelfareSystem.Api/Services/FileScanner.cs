using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
namespace WelfareSystem.Api.Services;
public interface IFileScanner { Task Check(byte[] bytes, CancellationToken ct); string Mode { get; } }
public sealed class FileScanner(IConfiguration config, IHostEnvironment environment) : IFileScanner {
    public string Mode => string.IsNullOrWhiteSpace(config["Documents:ScannerHost"]) ? "Unscanned development" : "ClamAV clean";
    public async Task Check(byte[] bytes, CancellationToken ct) {
        var host = config["Documents:ScannerHost"];
        if (string.IsNullOrWhiteSpace(host)) {
            if (environment.IsDevelopment()) return;
            throw new IOException("Malware scanning must be configured before production uploads are accepted.");
        }
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(TimeSpan.FromSeconds(30));
        try {
            using var client = new TcpClient(); await client.ConnectAsync(host, config.GetValue("Documents:ScannerPort", 3310), timeout.Token);
            await using var stream = client.GetStream(); await stream.WriteAsync(Encoding.ASCII.GetBytes("zINSTREAM\0"), timeout.Token);
            var length = new byte[4];
            for (var offset = 0; offset < bytes.Length; offset += 65536) {
                var size = Math.Min(65536, bytes.Length - offset); BinaryPrimitives.WriteInt32BigEndian(length, size);
                await stream.WriteAsync(length, timeout.Token); await stream.WriteAsync(bytes.AsMemory(offset, size), timeout.Token);
            }
            await stream.WriteAsync(new byte[4], timeout.Token);
            var response = new List<byte>(); var buffer = new byte[1];
            while (response.Count < 2048 && await stream.ReadAsync(buffer, timeout.Token) != 0 && buffer[0] != 0 && buffer[0] != 10) response.Add(buffer[0]);
            var text = Encoding.UTF8.GetString(response.ToArray());
            if (text.EndsWith(" FOUND", StringComparison.Ordinal)) throw new InvalidDataException("The scanner rejected this file. Upload clean supporting evidence.");
            if (text != "stream: OK") throw new IOException("The scanner could not verify this upload.");
        } catch (Exception error) when (error is SocketException or OperationCanceledException) { throw new IOException("File scanning is temporarily unavailable. Retry later.", error); }
    }
}
