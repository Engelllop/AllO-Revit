using Autodesk.Revit.DB;
using System;
using System.Collections.Concurrent;

namespace AllO.Services;

/// <summary>
/// Simple in-memory cache keyed by Document + query key, with expiration.
/// </summary>
public static class DocumentCache
{
    private sealed class CacheKey : IEquatable<CacheKey>
    {
        public int DocHash { get; }
        public string Query { get; }

        public CacheKey(int docHash, string query)
        {
            DocHash = docHash;
            Query = query;
        }

        public bool Equals(CacheKey? other)
        {
            if (other is null) return false;
            return DocHash == other.DocHash && Query == other.Query;
        }

        public override bool Equals(object? obj) => Equals(obj as CacheKey);
        public override int GetHashCode()
        {
            unchecked
            {
                return (DocHash * 397) ^ (Query?.GetHashCode() ?? 0);
            }
        }
    }

    private sealed class CacheEntry
    {
        public object Data { get; }
        public DateTime CachedAt { get; }

        public CacheEntry(object data, DateTime cachedAt)
        {
            Data = data;
            CachedAt = cachedAt;
        }
    }

    private static readonly ConcurrentDictionary<CacheKey, CacheEntry> _cache = new();
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    public static T GetOrAdd<T>(Document doc, string key, Func<T> factory, TimeSpan? ttl = null)
    {
        var cacheKey = new CacheKey(doc.GetHashCode(), key);
        var expiry = ttl ?? DefaultTtl;

        if (_cache.TryGetValue(cacheKey, out var entry) && DateTime.Now - entry.CachedAt < expiry)
        {
            return (T)entry.Data;
        }

        var data = factory();
        _cache[cacheKey] = new CacheEntry(data!, DateTime.Now);
        return data;
    }

    public static void Invalidate(Document doc)
    {
        var hash = doc.GetHashCode();
        foreach (var key in _cache.Keys)
        {
            if (key.DocHash == hash)
                _cache.TryRemove(key, out _);
        }
    }

    public static void Clear() => _cache.Clear();
}
