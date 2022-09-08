using System.Threading;
using static ProductImageFactory.Infrastructure.MemorizeFunctionality;
using ProductImageFactory.Infrastructure;
namespace ProductImageFactory;

internal class ProductImageFactoryMemorized : IProductImageFactory
{
  private readonly Func<Uri, CancellationToken, ValueTask<ProductImage>> _cachingFactory;
  public ProductImageFactoryMemorized(IDateProvider dateProvider, IProductImageUncachedFactory productImageUncachedFactory, IProductImageFactoryConfig config)
  {
    var getImage = async ValueTask<ProductImage> (Uri uri, CancellationToken c) => await Task.Run(() => productImageUncachedFactory.Create(uri),c);
    _cachingFactory = MemorizeWithStaleTime<Uri, ProductImage>(config.StaleTime, dateProvider.GetNow, getImage,config.CacheCapacity)
                      .LockFuncWith(new object());
  }
  public ProductImage Create(Uri uri) => _cachingFactory(uri, new CancellationToken()).Result; // wait for cached task
}