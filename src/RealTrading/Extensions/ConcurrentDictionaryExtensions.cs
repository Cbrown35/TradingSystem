using System.Collections.Concurrent;

namespace TradingSystem.RealTrading.Extensions;

public static class ConcurrentDictionaryExtensions
{
    public static TValue GetOrAdd<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue) where TKey : notnull
    {
        return dictionary.GetOrAdd(key, _ => defaultValue);
    }

    public static bool TryRemove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, out TValue? value) where TKey : notnull
    {
        return dictionary.TryRemove(key, out value);
    }
}
