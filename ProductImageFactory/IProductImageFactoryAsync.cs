namespace ProductImageFactory
{
  public interface IProductImageFactoryAsync
  {
    ValueTask<ProductImage> CreateAsync(Uri uri);
  }
}