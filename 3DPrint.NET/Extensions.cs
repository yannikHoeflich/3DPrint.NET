namespace _3DPrint.NET;

public static class Extensions {
    public static long ToLong<TValue>(this TValue value) where TValue : Enum
    => Convert.ToInt64(value);
}
