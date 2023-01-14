using System.Diagnostics;

namespace _3DPrint.NET;
public static class ExtensionMethods {
    public static long ToLong<TValue>(this TValue value) where TValue : Enum
    => Convert.ToInt64(value);

    public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TValue> createValue) {
        if(dictionary.TryGetValue(key, out TValue value)) return value;

        value = createValue();
        dictionary.Add(key, value);
        return value;
    }

    public static Type GetType(string fullName) {
        Debug.Assert(fullName != null);

        foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            Type t = assembly.GetType(fullName, false);
            if (t != null)
                return t;
        }
        throw new ArgumentException("Type " + fullName + " doesn't exist in the current app domain");
    }
}
