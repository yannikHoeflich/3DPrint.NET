using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Http.HttpResults;

namespace _3DPrint.NET.Data;
[Flags]
public enum SerialCode : long {
    None = 0,
    All = int.MaxValue,
    Unknown = 1 << 0,
    Ok = 1 << 1, // Ok
    GCode = 1 << 2, // N000 [GCode]
    Temperature = 1 << 3, // T:
    Busy = 1 << 4, // echo:busy: processing
    ActionNotification = 1 << 5, // echo:busy: processing
    PositionReport = 1 << 6, // X:0.40 Y:20.00 Z:0.30 E:0.00 Count X:8 Y:16000 Z:-317
    ActionCancelPrint = 1 << 7, // //action:cancel
    SizeResponse = 1 << 8, // //Min:  X0.00 Y0.00 Z0.00   Max:  X245.00 Y230.00 Z250.00
    ActionPausePrint = 1 << 9, // //action:pause
}

public static class SerialCodeExtensions {
    private static readonly (Regex Regex, SerialCode Code)[] s_parses = new (Regex Regex, SerialCode Code)[] {
        (new (@"^(N\d+ )?[GM]\d+.*$", RegexOptions.Compiled), SerialCode.GCode),
        (new (@"^([TB]:\d+\.\d{2} /\d+\.\d{2} ?)+([TB]?@:\d+ ?)+( W:[\d?]+)?$", RegexOptions.Compiled), SerialCode.Temperature),
        (new (@"^echo:busy: processing$", RegexOptions.Compiled), SerialCode.Busy),
        (new (@"^([XYZE]:-?\d+\.\d{2} ?)+Count ([XYZ]:-?\d+ ?)+$", RegexOptions.Compiled), SerialCode.PositionReport),
        (new (@"^((Min|Max):  ([XYZ]\d+\.\d{2} *)+)+$", RegexOptions.Compiled), SerialCode.SizeResponse),
        (new (@"^//action:notification .+$", RegexOptions.Compiled), SerialCode.ActionNotification),
        (new (@"^//action:pause$", RegexOptions.Compiled), SerialCode.ActionPausePrint),
        (new (@"^//action:cancel$", RegexOptions.Compiled), SerialCode.ActionCancelPrint),
        };
    internal static SerialCode Parse(string message) {
        if (message.StartsWith("ok"))
            return SerialCode.Ok;

        return s_parses.FirstOrDefault(x => x.Regex.IsMatch(message), (new (""), SerialCode.Unknown)).Code;
    }
}