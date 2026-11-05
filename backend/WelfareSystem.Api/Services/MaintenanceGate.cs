namespace WelfareSystem.Api.Services;
public sealed class MaintenanceGate {
    private readonly object sync = new(); private bool enabled; private int writers;
    public bool Enabled { get { lock (sync) return enabled; } }
    public int ActiveWriters { get { lock (sync) return writers; } }
    public IDisposable? TryWrite() { lock (sync) { if (enabled) return null; writers++; return new Lease(this); } }
    public async Task<bool> Set(bool value, CancellationToken ct) {
        lock (sync) enabled = value;
        if (!value) return true;
        var until = DateTime.UtcNow.AddSeconds(30);
        while (ActiveWriters > 0 && DateTime.UtcNow < until) await Task.Delay(100, ct);
        return ActiveWriters == 0;
    }
    private sealed class Lease(MaintenanceGate gate) : IDisposable {
        private int disposed;
        public void Dispose() { if (Interlocked.Exchange(ref disposed, 1) == 0) lock (gate.sync) gate.writers--; }
    }
}
