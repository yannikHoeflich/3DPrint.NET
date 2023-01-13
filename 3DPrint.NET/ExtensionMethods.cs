namespace _3DPrint.NET;

public static class ExtensionMethods {
    public static long ToLong<TValue>(this TValue value) where TValue : Enum
    => Convert.ToInt64(value);
}
