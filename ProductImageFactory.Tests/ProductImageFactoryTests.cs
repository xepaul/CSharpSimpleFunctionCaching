using Xunit;
using System;
using ProductImageFactory;
using Moq;
using System.Linq;
namespace ProductImageFactoryTests
{
  public class ProductImageFactoryTests
  {
    [Fact]
    public void TestImageCacheMemorizes()
    {
      //AAA
      //Arrange
      var uri1 = new System.Uri("http://a.com");
      var uri2 = new System.Uri("http://b.com");
      var uri3 = new System.Uri("http://c.com");
      var uri4 = new System.Uri("http://d.com");
      var dateTime = new System.DateTime(1900);
      var staleTime = TimeSpan.FromMinutes(10);
      var cacheCapacity = 3;
      var mDataProvider = new Mock<IDateProvider>();
      mDataProvider.Setup(m => m.GetNow()).Returns(dateTime);

      var mCreator = new Mock<IProductImageUncachedFactory>();
      mCreator.Setup(m => m.Create(It.IsAny<System.Uri>()))
            .Returns<System.Uri>(x => (new ProductImage(x)));

      var config = Mock.Of<IProductImageFactoryConfig>(m => m.StaleTime == staleTime
                                                            && m.CacheCapacity == cacheCapacity);

      //Act
      var factory = new ProductImageFactory.ProductImageFactoryMemorized(mDataProvider.Object, mCreator.Object, config);

      var a = factory.Create(uri1); // load uri1
      var a1 = factory.Create(uri1);
      var b = factory.Create(uri2); //load uri2
      var b2 = factory.Create(uri2);
      var a2 = factory.Create(uri1);
      var c = factory.Create(uri3); // load uri3
      var d = factory.Create(uri4); // load uri4, cache size breach, uri1 removed from cache, it was accessed last
      var b3 = factory.Create(uri2); // load uri2

      //Assert
      Assert.Same(a, a1);
      Assert.NotSame(a, b);
      Assert.Same(b, b2);
      Assert.Same(a, a2);
      Assert.NotSame(b, b3);

      Mock.Get(config).Verify(m => m.StaleTime, Times.Exactly(1));
      mCreator.Verify(m => m.Create(It.IsAny<Uri>()), Times.Exactly(5));
    }

    [Fact]
    public void TestImageCacheMemorizesWithStaleDateTime()
    {
      //Arrange
      var dateTime = new System.DateTime(1900, 12, 1);
      var uri1 = new System.Uri("http://a.com");

      var config = Mock.Of<IProductImageFactoryConfig>(m => m.StaleTime == TimeSpan.FromMinutes(10)
                                                            && m.CacheCapacity == 2);
      var mDataProvider = new Mock<IDateProvider>();
      mDataProvider.Setup(m => m.GetNow()).Returns(dateTime);

      var mProductImageUncachedFactory = new Mock<IProductImageUncachedFactory>();
      mProductImageUncachedFactory.Setup(m => m.Create(It.IsAny<System.Uri>()))
            .Returns<System.Uri>(x => new ProductImage(x));

      // Act with arrange
      var uut = new ProductImageFactory.ProductImageFactoryMemorized(mDataProvider.Object, mProductImageUncachedFactory.Object, config);

      var imageA = uut.Create(uri1);

      mDataProvider.Setup(m => m.GetNow()).Returns(dateTime.AddMinutes(10).AddMilliseconds(2));

      var imageB = uut.Create(uri1);
      var imageC = uut.Create(uri1);

      // Assert
      Assert.NotSame(imageA, imageB);
      Assert.Same(imageB, imageC);
      Mock.Get(config).Verify(m => m.StaleTime, Times.Exactly(1));
      mProductImageUncachedFactory.Verify(m => m.Create(It.IsAny<Uri>()), Times.Exactly(2));
    }

    [Fact]
    public void TestImageCacheMemorizesWithStaleDateTimeAAA() //completely AAA style, Act Arrange Assert, Act isn't merged with arrange
    {
      //Arrange
      var dateTime = new System.DateTime(1900, 12, 1);
      var uri1 = new System.Uri("http://a.com");
      var timeSpan = TimeSpan.FromMinutes(10);
      var cacheCapacity = 2;

      var accessSequence = new[]{
        (uri: uri1, timeInc :TimeSpan.Zero)
        ,(uri: uri1, timeInc :TimeSpan.FromMinutes(10).Add(TimeSpan.FromMilliseconds(1)))
        ,(uri: uri1, timeInc :TimeSpan.Zero)
        };

      var config = Mock.Of<IProductImageFactoryConfig>(m => m.StaleTime == timeSpan
                                                            && m.CacheCapacity == cacheCapacity );
      
      var mDataProvider = new Mock<IDateProvider>();
      mDataProvider.Setup(m => m.GetNow()).Returns(() => dateTime);

      var mProductImageUncachedFactory = new Mock<IProductImageUncachedFactory>();
      mProductImageUncachedFactory.Setup(m => m.Create(It.IsAny<System.Uri>()))
            .Returns<System.Uri>(x => new ProductImage(x));

      // Act
      var uut = new ProductImageFactory.ProductImageFactoryMemorized(mDataProvider.Object, mProductImageUncachedFactory.Object, config);

      var productAccesses = accessSequence
        .Do(instruction => dateTime += instruction.timeInc)
        .Select(instruction => uut.Create(instruction.uri))
        .ToList(); ;

      var imageA = productAccesses[0];
      var imageB = productAccesses[1];
      var imageC = productAccesses[2];

      // Assert
      Assert.NotSame(imageA, imageB);
      Assert.Same(imageB, imageC);
      Mock.Get(config).Verify(m => m.StaleTime, Times.Exactly(1));
      Mock.Get(config).Verify(m => m.CacheCapacity, Times.Exactly(1));
      mProductImageUncachedFactory.Verify(m => m.Create(It.IsAny<Uri>()), Times.Exactly(2));
    }
  }
}