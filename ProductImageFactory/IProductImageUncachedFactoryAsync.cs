using System.Threading;

namespace ProductImageFactory
{
  public interface IProductImageUncachedFactoryAsync
  {
    // this should be constructed to timeout as desired
    ValueTask<ProductImage> CreateAsync(Uri uri, CancellationToken token);
  }

  public interface IProductImageUncachedFactory
  {
    ProductImage Create(Uri uri);
  }

  public class ProductImageUncachedFactory : IProductImageUncachedFactory
  {
    ProductImage IProductImageUncachedFactory.Create(Uri uri)
    {
      //Getting image over a slow link takes ages..
      Thread.Sleep(1000);
      return new ProductImage(uri);
    }
  }

}
