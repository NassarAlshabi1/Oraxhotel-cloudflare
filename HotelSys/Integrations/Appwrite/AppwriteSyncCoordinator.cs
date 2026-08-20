using System;
using System.Collections.Concurrent;
using System.Threading;

namespace HotelSys.Integrations.Appwrite;

/// <summary>
/// يمنع تشغيل مزامنتين لنفس الكيان في العملية نفسها، خصوصاً بين المزامنة الدورية
/// والاستدعاء اليدوي من لوحة الإدارة. القفل لكل كيان مستقل ولا يمنع مزامنة كيان آخر.
/// </summary>
public sealed class AppwriteSyncCoordinator
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new(StringComparer.OrdinalIgnoreCase);

    public bool TryEnter(string entity, out IDisposable lease)
    {
        var gate = _gates.GetOrAdd(entity, static _ => new SemaphoreSlim(1, 1));
        if (!gate.Wait(0))
        {
            lease = NoopLease.Instance;
            return false;
        }

        lease = new SyncLease(gate);
        return true;
    }

    private sealed class SyncLease : IDisposable
    {
        private readonly SemaphoreSlim _gate;
        private int _released;

        public SyncLease(SemaphoreSlim gate) => _gate = gate;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                _gate.Release();
            }
        }
    }

    private sealed class NoopLease : IDisposable
    {
        public static readonly NoopLease Instance = new();
        public void Dispose() { }
    }
}
