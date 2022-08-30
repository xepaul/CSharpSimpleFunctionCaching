using System;

namespace ProductImageFactory
{
    public class ProductImage
    {
        private readonly Uri _uri;

        public ProductImage(Uri uri)
        {
            _uri = uri;
        }

        public override string ToString()
        {
            return _uri.ToString();
        }
    }
}
