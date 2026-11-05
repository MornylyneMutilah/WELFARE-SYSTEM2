using System.Security.Cryptography;
using System.Text;

namespace WelfareSystem.Api.Services;

// Single-instance development store. Replace with an atomic shared store before running multiple API replicas.
public sealed class ExternalAuthFlowStore(TimeProvider clock)
{
    private readonly Dictionary<string, (object Value, DateTimeOffset Expires)> _entries = new();
    private readonly object _gate = new();
    public string Put<T>(T value, TimeSpan lifetime) where T : class
    {
        lock (_gate) {
            foreach (var key in _entries.Where(e => e.Value.Expires <= clock.GetUtcNow()).Select(e => e.Key).ToArray()) _entries.Remove(key);
            if (_entries.Count >= 10000) throw new InvalidOperationException("Sign-in capacity reached. Try again shortly.");
            var token = RandomToken();
            _entries[Key<T>(token)] = (value, clock.GetUtcNow().Add(lifetime));
            return token;
        }
    }
    public T? Take<T>(string? token, Func<T, bool> matches) where T : class
    {
        if (string.IsNullOrEmpty(token) || token.Length > 128) return null;
        lock (_gate) {
            var key = Key<T>(token);
            if (!_entries.TryGetValue(key, out var entry)) return null;
            if (entry.Expires <= clock.GetUtcNow()) { _entries.Remove(key); return null; }
            if (entry.Value is not T value || !matches(value)) return null;
            _entries.Remove(key);
            return value;
        }
    }
    private static string Key<T>(string value) => typeof(T).Name + ":" + Hash(value);
    public static string RandomToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    public static bool Equal(string left, string right) => CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(left)), SHA256.HashData(Encoding.UTF8.GetBytes(right)));
}
