namespace ProductImageFactory
{
  public interface IProductImageFactoryConfig
  {
    /// <summary>
    /// Time after which a cache item is deemed stale and shoudl be recomputee
    /// </summary>
    TimeSpan StaleTime { get; }
    /// <summary>
    /// // maximum number of items the cache can hold
    /// </summary>
    int CacheCapacity { get; }
  }
}
