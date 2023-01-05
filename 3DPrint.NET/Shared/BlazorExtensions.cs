using System.Text.RegularExpressions;
using _3DPrint.NET.Connection;

namespace _3DPrint.NET.Shared;
public static class BlazorExtensions {
    public static string GetColorClass(this ConnectionState connectionState) {
        return connectionState switch {
            ConnectionState.NotConnected => "font-red",
            ConnectionState.Connected => "font-green",
            ConnectionState.NoResponse => "font-red",
            ConnectionState.Error => "font-red",
            _ => "font-red"
        };
    }

    private static Regex s_pascalCaseRegex = new Regex(@"[a-z][A-Z]", RegexOptions.Compiled);
    public static string ToWords<T>(this T value) where T : Enum {
        return s_pascalCaseRegex.Replace(value.ToString(), m => m.Value[0] + " " + char.ToLower(m.Value[1]));
    }
}
