using System.Collections.Immutable;

namespace ProductImageFactory.Infrastructure;

public static class ImmutableDictionaryExts
{
  public static (ImmutableDictionary<TKey, TValue>, TValue) AddOrUpdate<TKey, TValue>(this ImmutableDictionary<TKey, TValue> d, TKey key, Func<TKey, TValue> factory, Func<TKey, TValue, TValue> updater)
  {
    if (d.TryGetValue(key, out var o))
    {
      var updatedValue = updater(key, o);
      return (d.SetItem(key, updatedValue), updatedValue);
    }
    else
    {
      var newValue = factory(key);
      return (d.Add(key, newValue), newValue);
    }
  }

}
