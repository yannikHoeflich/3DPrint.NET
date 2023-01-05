using System.Drawing;

using _3DPrint.NET.Data;
using _3DPrint.NET.Data.Processors;

namespace _3DPrint.NET.Services;
public static class PrinterInitializer {
    public static async Task Init() {
        await Printer.Current.ExcecuteGcodeAsync(new GCodeMessage(GCode.TemperatureAutoReport, "S4"));
        await Printer.Current.ExcecuteGcodeAsync(new GCodeMessage(GCode.PositionAutoReport, "S4"));

        SerialMessage[] responses = await Printer.Current.ExcecuteGcodeAndWait(new GCodeMessage(GCode.SoftwareEndstops));
        PrinterStateService.Current.Size = ResponseParser.ReadSizeResponse(responses.FirstOrDefault(x => x.Code == SerialCode.SizeResponse));
    }
}
