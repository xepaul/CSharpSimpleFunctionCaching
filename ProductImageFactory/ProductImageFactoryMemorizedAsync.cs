using System.Threading;
using static ProductImageFactory.MemorizeFunctionality;
namespace ProductImageFactory
{
  internal class ProductImageFactoryMemoizedAsync : IProductImageFactoryAsync
  {
    private readonly Func<Uri, CancellationToken, ValueTask<ProductImage>> _cachingFactory;
    public ProductImageFactoryMemoizedAsync(IDateProvider dateProvider, 
                                            IProductImageUncachedFactoryAsync productImageUncachedFactoryAsync,
                                            IProductImageFactoryConfig config) => 
    _cachingFactory = MemorizeWithStaleTime<Uri, ProductImage>(config.StaleTime, dateProvider.GetNow, 
                                                                              productImageUncachedFactoryAsync.CreateAsync,config.CacheCapacity);
    public ValueTask<ProductImage> CreateAsync(Uri uri) => _cachingFactory(uri, new CancellationToken());
  }
}