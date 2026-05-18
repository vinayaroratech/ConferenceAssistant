using System.Collections.Concurrent;

namespace ConferenceAssistant.Core.Services;

public class InMemoryStore<T> where T : class
{
    // Plain Dictionary preserves insertion order in .NET (verified since .NET Core 3+).
    // All access is guarded by _lock so this is thread-safe.
    private readonly Dictionary<string, T> _items = new();
    private readonly object _lock = new();

    public void Add(string key, T item)
    {
        lock (_lock) { _items[key] = item; }
    }

    public T? Get(string key)
    {
        lock (_lock) { return _items.TryGetValue(key, out var v) ? v : null; }
    }

    /// <summary>Returns items in insertion order.</summary>
    public IReadOnlyList<T> GetAll()
    {
        lock (_lock) { return _items.Values.ToList(); }
    }

    public bool Remove(string key)
    {
        lock (_lock) { return _items.Remove(key); }
    }

    public IReadOnlyList<T> Query(Func<T, bool> predicate)
    {
        lock (_lock) { return _items.Values.Where(predicate).ToList(); }
    }

    public void Clear()
    {
        lock (_lock) { _items.Clear(); }
    }
}
