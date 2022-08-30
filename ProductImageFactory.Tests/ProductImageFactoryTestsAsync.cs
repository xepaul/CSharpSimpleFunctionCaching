using Xunit;

using ProductImageFactory;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace ProductImageFactoryTests;

public class ProductImageFactoryTestsAsync
{
  [Fact]
  public async void TestImageCacheMemorizes()
  {
    //Arrange
    var uri1 = new System.Uri("http://a.com");
    var uri2 = new System.Uri("http://b.com2");
    var mDataProvider = new Mock<IDateProvider>();
    mDataProvider.Setup(m => m.GetNow()).Returns(new System.DateTime(1900));

    var mCreator = new Mock<IProductImageUncachedFactoryAsync>();
    mCreator.Setup(m => m.CreateAsync(It.IsAny<System.Uri>(), It.IsAny<CancellationToken>()))
          .Returns<System.Uri, CancellationToken>((x, c) => ValueTask.FromResult(new ProductImage(x)));
    var config = Mock.Of<IProductImageFactoryConfig>( m=> m.StaleTime == TimeSpan.FromMinutes(10)
                                                            && m.CacheCapacity == 2);

    var factory = new ProductImageFactory.ProductImageFactoryMemoizedAsync(mDataProvider.Object, mCreator.Object,config);

    var aAsync = factory.CreateAsync(uri1);
    var bAsync = factory.CreateAsync(uri1);
    var cAsync = factory.CreateAsync(uri2);

    var a = await aAsync;
    var b = await bAsync;
    var c = await cAsync;
    //Assert
    Assert.Same(a, b);
    Assert.NotSame(a, c);

    mCreator.Verify(m => m.CreateAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()), Times.Exactly(2));  // assert the real factroy is only called 3 times
  }
  [Fact]
  public async void TestImageCacheMemorizesWithStaleDateTime()
  {
    //Arrange
    var uri1 = new System.Uri("http://a.com");
    var uri2 = new System.Uri("http://b.com");
    var mDataProvider = new Mock<IDateProvider>();
    var startTime = new System.DateTime(1900, 12, 1);
    var staleTime = TimeSpan.FromMinutes(10);
    var capacity = 2;
    mDataProvider.Setup(m => m.GetNow()).Returns(startTime);

    var mCreator = new Mock<IProductImageUncachedFactoryAsync>();
    mCreator.Setup(m => m.CreateAsync(It.IsAny<System.Uri>(), It.IsAny<CancellationToken>()))
          .Returns<System.Uri, CancellationToken>((x, c) => ValueTask.FromResult(new ProductImage(x)));
          
    var config = Mock.Of<IProductImageFactoryConfig>( m=> m.StaleTime == staleTime
                                                            && m.CacheCapacity == capacity);
    //Arrange with Act
    var factory = new ProductImageFactory.ProductImageFactoryMemoizedAsync(mDataProvider.Object, mCreator.Object,config);
    var imageAAsync = factory.CreateAsync(uri1);
    mDataProvider.Setup(m => m.GetNow()).Returns(startTime.Add(staleTime).AddMilliseconds(1));

    var imageBAsync = factory.CreateAsync(uri1);
    var imageCAsync = factory.CreateAsync(uri1);
    var imageDAsync = factory.CreateAsync(uri2);

    var imageA = await imageAAsync;
    var imageB = await imageBAsync;
    var imageC = await imageCAsync;

    //Asset
    Assert.NotSame(imageA, imageB);
    Assert.Same(imageB, imageC);

    mCreator.Verify(m => m.CreateAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()), Times.Exactly(3)); // assert the real factroy is only called 3 times
  }
}