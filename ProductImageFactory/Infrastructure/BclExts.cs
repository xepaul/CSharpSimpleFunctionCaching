namespace ProductImageFactory.Infrastructure;

public static class BclExts
{
  public static Func<TArg, TResult> LockFuncWith<TArg, TResult>(this Func<TArg, TResult> f, object locker)
  {
    lock (locker)
      return a => f(a);
  }
  public static Func<TArg, TArg2, TResult> LockFuncWith<TArg, TArg2, TResult>(this Func<TArg, TArg2, TResult> f, object locker)
  {
    lock (locker)
      return (a, b) => f(a, b);
  }
}
