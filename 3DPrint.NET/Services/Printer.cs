using System.Globalization;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;

using _3DPrint.NET.Connection;
using _3DPrint.NET.Data;
using _3DPrint.NET.Data.Processors;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;

namespace _3DPrint.NET.Services;
public class Printer {

    public static event EventHandler? BeforePrintStart;
    public static event EventHandler? PrintFinished;
    public static event EventHandler? PrintCanceled;

    public static Printer Current { get; private set; }

    public PrinterStateService State { get; }
    public PrintingManager? CurrentPrint { get; private set; }

    public PrintState PrintState => CurrentPrint != null
                                    ? CurrentPrint.State
                                    : PrintState.None;

    public bool IsPrinting => PrintState != PrintState.None;

    public Printer(PrinterStateService printerState) {
        State = printerState;
        Current = this;
    }

    public async Task InitAsync() {
        if (State.ConnectionState == ConnectionState.Connected)
            await PrinterInitializer.Init();
    }

    public async Task MoveToAsync(double x = double.NaN, double y = double.NaN, double z = double.NaN, double extrude = double.NaN) {
        var moves = new List<string>();

        if (!double.IsNaN(x))
            moves.Add($"X{x.ToString(CultureInfo.InvariantCulture)}");

        if (!double.IsNaN(y))
            moves.Add($"Y{y.ToString(CultureInfo.InvariantCulture)}");

        if (!double.IsNaN(z))
            moves.Add($"Z{z.ToString(CultureInfo.InvariantCulture)}");

        if (!double.IsNaN(extrude))
            moves.Add($"E{extrude.ToString(CultureInfo.InvariantCulture)}");

        if (moves.Count == 0)
            return;
        await ExcecuteGcodeAsync(new GCodeMessage(GCode.LinearMoveNotPrinting, moves.ToArray()));
    }

    public async Task MoveByAsync(double x = 0, double y = 0, double z = 0, double extrude = 0) {
        var moves = new List<string>();

        if (x != 0)
            moves.Add($"X{(State.Position.X + x).ToString(CultureInfo.InvariantCulture)}");

        if (y != 0)
            moves.Add($"Y{(State.Position.Y + y).ToString(CultureInfo.InvariantCulture)}");

        if (z != 0)
            moves.Add($"Z{(State.Position.Z + z).ToString(CultureInfo.InvariantCulture)}");

        if (extrude != 0)
            moves.Add($"E{(State.Position.E + extrude).ToString(CultureInfo.InvariantCulture)}");

        if (moves.Count == 0)
            return;
        await ExcecuteGcodeAsync(new GCodeMessage(GCode.LinearMoveNotPrinting, moves.ToArray()));
    }

    public async Task SetTemperatuesAsync(double hotEnd = double.NaN, double bed = double.NaN) {
        if (!double.IsNaN(hotEnd))
            await ExcecuteGcodeAsync(new GCodeMessage(GCode.SetHotendTemperature, $"S{hotEnd.ToString(CultureInfo.InvariantCulture)}"));

        if (!double.IsNaN(bed))
            await ExcecuteGcodeAsync(new GCodeMessage(GCode.SetBedTemperature, $"S{bed.ToString(CultureInfo.InvariantCulture)}"));
    }

    public async Task SetTemperatuesAndWaitAsync(double hotEnd = double.NaN, double bed = double.NaN) {
        if (!double.IsNaN(hotEnd))
            await ExcecuteGcodeAsync(new GCodeMessage(GCode.WaitForHotendTemperature, $"S{hotEnd.ToString(CultureInfo.InvariantCulture)}"));

        if (!double.IsNaN(bed))
            await ExcecuteGcodeAsync(new GCodeMessage(GCode.WaitForBedTemperature, $"S{bed.ToString(CultureInfo.InvariantCulture)}"));
    }

    public async Task AutoHomeAsync() => await ExcecuteGcode(GCode.AutoHome);

    public async Task<double[,]> GetBedlevelingMeshAsync() {
        await ExcecuteGcodeAsync(new GCodeMessage(GCode.BedLevelingState, "L1"));
        SerialMessage[] rawData = await ExcecuteGcodeAndWait(new GCodeMessage(GCode.BedLevelingState, "V1"));
        return ResponseParser.ParseMesh(rawData.Select(x => x.Full));
    }

    public async Task SetFanSpeedAsync(double newValue) {
        int speed = (int)(newValue * 255);
        await ExcecuteGcode(GCode.SetFanSpeed, $"S{speed}");
    }

    public async Task Print(Stream gCode) {
        var reader = new StreamReader(gCode);
        var lines = new List<string>();
        string? line = null;
        while ((line = await reader.ReadLineAsync()) != null)
            lines.Add(line);

        await Print(lines);
    }

    public async Task Print(IEnumerable<string> gCode) {
        CurrentPrint = new PrintingManager(this, gCode);
        await CurrentPrint.StartAsync();
    }


    public Task ExcecuteGcode(GCode gCode, params string[] args) => ExcecuteGcodeAsync(new GCodeMessage(gCode, args));
    public Task ExcecuteGcode(string gCode) => ExcecuteGcodeAsync(new GCodeMessage(gCode));
    public async Task ExcecuteGcodeAsync(GCodeMessage gCodeMessage) {
        if (IsPrinting)
            throw new InvalidOperationException("The printer is currently printing, please wait for finish or cancel print.");
        await SerialMonitor.SendAsync(gCodeMessage);
    }

    public Task<SerialMessage[]> ExcecuteGcodeAndWait(string gCode, int timeout = 2000) => ExcecuteGcodeAndWait(new GCodeMessage(gCode), timeout);
    public async Task<SerialMessage[]> ExcecuteGcodeAndWait(GCodeMessage gCodeMessage, int timeout = 2000) {
        if (IsPrinting)
            throw new InvalidOperationException("The printer is currently printing, please wait for finish or cancel print.");

        return await SerialMonitor.SendAndWaitForResponseAsync(gCodeMessage, timeout);
    }
}
