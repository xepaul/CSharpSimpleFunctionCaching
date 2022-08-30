using System.Collections.Concurrent;
using static System.Math;
using System.Threading;
namespace ProductImageFactory;

public static class MemorizeFunctionality
{
  /// <summary>
  /// <para> Cache a functions results using the arguments as a key, don't use propogate cached items older than the stale time </para>
  /// <para> Todo: Should really consider the number of concurrent factorys that are allowed to run, especially if they block! </para>
  /// </summary>
  /// <typeparam name="TArgs"> function argument type</typeparam>
  /// <typeparam name="TValue"> function response type</typeparam>
  /// <param name="staleTime"> duration to specify when an item is stale andn should be recomputed</param>
  /// <param name="getTime">function to injec the current time</param>
  /// <param name="f"> the function to cache, should implement timeout logic</param>
  /// <param name="capacity"> the size of the cache</param>
  /// <returns> a caching version of the given function and a Idispoable to dispose of the cache</returns>
  public static Func<TArgs, CancellationToken, ValueTask<TValue>>  MemorizeWithStaleTime<TArgs, TValue>(
    TimeSpan staleTime, Func<DateTime> getTime, Func<TArgs, CancellationToken, ValueTask<TValue>> f, int? capacity = null)
  //where TRequest: IEquatable<TArgs> Uri doens't implement IEquatable // would wrap in another type ina fuly generic implementation, just remove for this poc
  {
    ConcurrentDictionary<TArgs, CacheEntry<TValue>> responseCache =
      new(EqualityComparer<TArgs>.Default);
    var age = 0L; // ensure if gettime returns the same time do two requests to "timestamp" for the entry is different, and ordered by making a tuple of this age and the time.

    var locker = new object();

    var fetchFromCache = (TArgs req, CancellationToken callingCancelationToken) =>
    {
      var timeNow = getTime();
      var createCacheEntry = (TArgs req) =>
      {
        var c = CancellationTokenSource.CreateLinkedTokenSource(callingCancelationToken);
        return new CacheEntry<TValue>(timeNow, (age++, timeNow), c, f(req, c.Token)); // timeouts for the request will be assumed to be done in f
      };
      var isCacheEntryStale = (CacheEntry<TValue> entry) => (timeNow - entry.cachedTime) > staleTime;
      var trimCacheSize = () =>
      {
        if (capacity is int cap && responseCache.Count > cap)
        {
          responseCache.Where(kv => isCacheEntryStale(kv.Value)
                                    //|| kv.Value.responseAsync.IsCanceled 
                                    || kv.Value.responseAsync.IsCompleted
                                        && kv.Value.responseAsync.IsFaulted
                                    //|| kv.Value.token.IsCancellationRequested
                                    )
                       .ToList()
                       .ForEach(e => responseCache.TryRemove(e)); // get rid of the stale items and bad entries before ordering everything

          responseCache.OrderBy(x => x.Value.accessHistory)
                         .Take(Max(0, responseCache.Count - cap))
                         .ToList()
                         .ForEach(e => responseCache.TryRemove(e));
        }
      };

      var getFromCache = (TArgs req) =>
      {
        var response = responseCache.AddOrUpdate(req, createCacheEntry,
                                  (req, cachedItem) =>
                                  {
                                    if (!isCacheEntryStale(cachedItem)
                                        && !cachedItem.responseAsync.IsFaulted
                                          && !cachedItem.responseAsync.IsCanceled)
                                      return cachedItem with { accessHistory = (age++, timeNow) };
                                    else
                                    {
                                      if (!cachedItem.responseAsync.IsCompleted && !cachedItem.responseAsync.IsFaulted)
                                        cachedItem.token.Cancel();
                                      return createCacheEntry(req);
                                    }
                                  });
        trimCacheSize();
        return response;
      };
      lock (locker) //f returns a ValueTask its not awaited so calling f should be fast, lock to ensure we don't call f and the result gets dumped 
        return getFromCache(req).responseAsync;
    };
    return fetchFromCache;
  }

  private record struct CacheEntry<TResponse>(DateTime cachedTime, (long age, DateTime lastAccesed) accessHistory,
                                               CancellationTokenSource token, ValueTask<TResponse> responseAsync);
}

public static class MemorizeFunctionalityExts
{

  // Generic function to cache async functions that is recomputed after a specified stale time
  public static Func<TRequest, CancellationToken, ValueTask<TResponse>> MemorizeFuncWithStaleTime<TRequest, TResponse>(
           this Func<TRequest, CancellationToken, ValueTask<TResponse>> f,
           TimeSpan staleTime, Func<DateTime> getTime, int? capacity = null) => 
      MemorizeFunctionality.MemorizeWithStaleTime<TRequest, TResponse>(staleTime, getTime, f, capacity);
}