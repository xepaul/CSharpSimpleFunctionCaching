using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;
using ProductImageFactory;
using Xunit;

namespace ProductImageFactoryTests
{
  public class MemorizingCachesTests
  {
    [Fact]
    public async void TestCacheMemorizesWithStaleDateTimeGenericMemorizeFunc()
    {
      //AAA arrane act assert
      // Arrange
      var uri1 = new System.Uri("http://a1.com");
      var uri2 = new System.Uri("http://a2.com");
      var uri3 = new System.Uri("http://a3.com");
      var uri4 = new System.Uri("http://a4.com");
      var dateTimeStart = new System.DateTime(1900, 12, 1);
      var dateTimeSecond = new System.DateTime(1900, 12, 2);
      var dateTimeThird = new System.DateTime(1900, 12, 3);
      var dateTime = dateTimeStart;

      var getTime = () => dateTime;
      var moveTime = () => dateTime += TimeSpan.FromSeconds(1);
      var createCalls = 0;
      var uriCalls = new List<(System.Uri uri, DateTime time)>();
      var factoryFunc = (System.Uri uri, CancellationToken c) =>
      {
        uriCalls.Add((uri, getTime()));
        createCalls++;
        return ValueTask.FromResult(new ProductImage(uri));
      };
      var factoryCached = MemorizeFunctionality.MemorizeWithStaleTime(TimeSpan.FromMinutes(10), getTime, factoryFunc, 2);

      var c = new CancellationToken();
      var image1aaAsync = factoryCached(uri1, c);  //1 load
      var image1aa = await image1aaAsync;

      dateTime = new System.DateTime(1900, 12, 2);

      var image1aAsync = factoryCached(uri1, c); //1 reload
      moveTime();
      var image1bAsync = factoryCached(uri1, c); //1
      moveTime();
      var image2aAsync = factoryCached(uri2, c); //1,2 load 2
      moveTime();
      var image3aAsync = factoryCached(uri3, c); //2,3 load 3
      moveTime();
      var image1cAsync = factoryCached(uri1, c);//3,1 load 1
      var image4aAsync = factoryCached(uri4, c);//1,4 load 4
      moveTime();
      var image1dAsync = factoryCached(uri1, c);// 4,1 

      var image3bAsync = factoryCached(uri3, c); //4,3 load 3 unload 1
      dateTime = dateTimeThird;
      var image2bAsync = factoryCached(uri2, c); //2 load 2 unload 3 its stale

      moveTime();
      moveTime();

      var image1eAsync = factoryCached(uri1, c);// 4,1 


      var image1a = await image1aAsync;
      var image1b = await image1bAsync;
      var image2a = await image2aAsync;
      var image3a = await image3aAsync;
      var image1c = await image1cAsync;
      var image4a = await image4aAsync;
      var image1d = await image1dAsync;
      var image3b = await image3bAsync;
      var image2b = await image2bAsync;


      var expectedCalls = new (Uri uri, DateTime time)[]{
        (uri1, dateTimeStart),
        (uri1, dateTimeSecond),
        (uri2, dateTimeSecond.AddSeconds(2)),
        (uri3, dateTimeSecond.AddSeconds(3)),
        (uri1, dateTimeSecond.AddSeconds(4)),
        (uri4, dateTimeSecond.AddSeconds(4)),
        (uri3, dateTimeSecond.AddSeconds(5)),
        (uri2,dateTimeThird),
        (uri1, dateTimeThird.AddSeconds(2)), // should activate stale items removal first
      
        };


      image1aa.Should().NotBeSameAs(image1a);
      image1c.Should().BeSameAs(image1d);
      createCalls.Should().Be(9);
      uriCalls.Should().BeEquivalentTo(expectedCalls);
    }



    [Fact]
    public async void TestCacheMemorizesWithStaleDateTimeGenericMemorizeFuncAAA()
    {
      // AAA Arrange Act Assert - Act isn't combined with Arrange
      // Arrange
      var uri1 = new System.Uri("http://a1.com");
      var uri2 = new System.Uri("http://a2.com");
      var uri3 = new System.Uri("http://a3.com");
      var uri4 = new System.Uri("http://a4.com");
      var dateTimeStart = new System.DateTime(1900, 12, 1);
      var dateTimeSecond = new System.DateTime(1900, 12, 2);
      var dateTimeThird = new System.DateTime(1900, 12, 3);
      var dateTime = dateTimeStart;

      var getTime = () => dateTime;

      var calls = new (Uri uri, DateTime time, bool shouldFault, bool shouldCancel)[]{
        (uri1, dateTimeStart,false,false),
        (uri1, dateTimeSecond,false,false),
        (uri2, dateTimeSecond.AddSeconds(2),false,false),
        (uri3, dateTimeSecond.AddSeconds(3),false,false),
        (uri1, dateTimeSecond.AddSeconds(4),false,false),
        (uri4, dateTimeSecond.AddSeconds(4),false,false),
        (uri3, dateTimeSecond.AddSeconds(5),false,false),
        (uri2,dateTimeThird,false,false),
        (uri1, dateTimeThird.AddSeconds(2),false,false), // should activate stale items removal first
        (uri3, dateTimeThird.AddSeconds(3),true,false), // test faulted items get eliminated from cache
        (uri3, dateTimeThird.AddSeconds(3),false,false),//should reload
        (uri2,dateTimeThird.AddSeconds(3),false,true),
        (uri4, dateTimeThird.AddSeconds(3),false,false),
        (uri2,dateTimeThird.AddSeconds(3),false,false), //should reload

        };

      var moveTime = () => dateTime += TimeSpan.FromSeconds(1);
      var createCalls = 0;
      var shouldFault = false;
      var shouldCancel = false;
      var uriCalls = new List<(System.Uri uri, DateTime time)>();
      var factoryFunc = (System.Uri uri, CancellationToken c) =>
      {
        uriCalls.Add((uri, getTime()));
        createCalls++;
        if (shouldFault)
          return ValueTask.FromException<ProductImage>(new Exception("fault"));
        if (shouldCancel)
        {
          return new Func<ValueTask<ProductImage>>(async () =>
          {
            var f = async () =>
            {
              while (!c.IsCancellationRequested)
              {
                await Task.Delay(200);
              }
            };
            await f();

            return await ValueTask.FromCanceled<ProductImage>(c);
          })();
          //return ValueTask.FromCanceled<ProductImage>(c);
        }

        return ValueTask.FromResult(new ProductImage(uri));
      };
      var factoryCached = MemorizeFunctionality.MemorizeWithStaleTime(TimeSpan.FromMinutes(10), getTime, factoryFunc, 2);
      var c = new CancellationToken();

      //Act
      var results = await calls.ToAsyncEnumerable().SelectAwait(async x =>
       {
         dateTime = x.time;
         shouldFault = x.shouldFault;
         shouldCancel = x.shouldCancel;
         try
         {
           if (x.shouldCancel)
           {
             var cc = new CancellationTokenSource();
             cc.CancelAfter(500);
             return await factoryCached(x.uri, cc.Token);
           }
           else
             return await factoryCached(x.uri, c);
         }
         catch (Exception _)
         {
           return null;
         }

       })
       .ToListAsync();

      //Assert
      var expectedCalls = new (Uri uri, DateTime time)[]{
        (uri1, dateTimeStart),
        (uri1, dateTimeSecond),
        (uri2, dateTimeSecond.AddSeconds(2)),
        (uri3, dateTimeSecond.AddSeconds(3)),
        (uri1, dateTimeSecond.AddSeconds(4)),
        (uri4, dateTimeSecond.AddSeconds(4)),
        (uri3, dateTimeSecond.AddSeconds(5)),
        (uri2,dateTimeThird),
        (uri1, dateTimeThird.AddSeconds(2)), // should activate stale items removal first
        (uri3, dateTimeThird.AddSeconds(3)),
        (uri3, dateTimeThird.AddSeconds(3)),
        (uri2, dateTimeThird.AddSeconds(3)),
        (uri4, dateTimeThird.AddSeconds(3)),
        (uri2, dateTimeThird.AddSeconds(3)),
        };

      createCalls.Should().Be(14);
      uriCalls.Should().BeEquivalentTo(expectedCalls);
    }

    [Fact]
    public async void TestImageCacheMemorizesWithStaleDateTimeGenericMemorizeFuncCapacity()
    {
      var dateTime = new System.DateTime(1900, 12, 1);
      var cacheCapacity = 2;
      var staleTime = TimeSpan.FromMinutes(10);
      var uri1 = new System.Uri("http://a1.com");
      var uri2 = new System.Uri("http://a2.com");
      var uri3 = new System.Uri("http://a3.com");
      var uri4 = new System.Uri("http://a4.com");

      var getTime = () => dateTime;

      var moveTime = () => dateTime += TimeSpan.FromSeconds(1);

      var createCalls = 0;
      var uriCalls = new List<Uri>();
      var factoryFunc = (System.Uri uri, CancellationToken c) =>
      {
        createCalls++;
        uriCalls.Add(uri);
        return ValueTask.FromResult(new ProductImage(uri));
      };
      var factoryCached = MemorizeFunctionality.MemorizeWithStaleTime<Uri, ProductImage>(staleTime, getTime, factoryFunc, cacheCapacity);

      var c = new CancellationToken();
      var image1aAsync = factoryCached(uri1, c); // load uri1
      var image1bAsync = factoryCached(uri1, c);
      var image1cAsync = factoryCached(uri1, c);
      var image2aAsync = factoryCached(uri2, c); //load uri2
      var image3aAsync = factoryCached(uri3, c); //load uri3 remove uri1
      var image2bAsync = factoryCached(uri2, c);
      var image1dAsync = factoryCached(uri1, c); // load uri1

      var image1a = await image1aAsync;
      var image1b = await image1bAsync;
      var image1c = await image1cAsync;
      var image2a = await image2aAsync;
      var image3a = await image3aAsync;
      var image2b = await image2bAsync;
      var image1d = await image1dAsync;

      createCalls.Should().Be(4);
      image1a.Should().NotBeSameAs(image1d);
    }
  }
}
