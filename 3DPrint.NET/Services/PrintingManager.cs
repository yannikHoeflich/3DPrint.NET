using _3DPrint.NET.Connection;
using _3DPrint.NET.Data;
using _3DPrint.NET.Data.EventArguments;
using _3DPrint.NET.Data.Processors;
using _3DPrint.NET.Data.Values;

namespace _3DPrint.NET.Services;
public class PrintingManager : IAsyncDisposable {
    private readonly Printer _printer;
    private string[]? _gCode;
    private TemperatureContainer _beforePauseTemperatures;
    private int _beforePauseFanSpeed;
    private DateTime _printStart;

    private DateTime _lastAnalyzed;

    public event EventHandler<PrintEventArgs>? PrintStateChanged;

    public int CurrentGCodeLine {get; private set; } = 0;
    public PrintState State { get; private set; } = PrintState.None;
    public TimeSpan TotalTime { get; private set; }
    public TimeSpan ElapsedTime => DateTimeGetter.Now - _printStart;

    public PrintingManager(Printer printer, IEnumerable<string> gCode) {
        _printer = printer;
        _gCode = gCode.Select(x => x.Split(';')[0]).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
    }


    public async Task StartAsync() {
        if (_gCode == null)
            return;

        AnalyzePrintTime();
        State = PrintState.Printing;

        PrintStateChanged?.Invoke(this, new PrintEventArgs(State, 0, CurrentGCodeLine));

        _printStart = DateTimeGetter.Now;
        CurrentGCodeLine = 0;
        SerialMonitor.RegisterDefaultMessageReceived(SerialCode.Ok, AppendGCodeAsync);
        SerialMonitor.RegisterDefaultMessageReceived(SerialCode.ActionPausePrint, PausePrintActionCommandAsync);
        SerialMonitor.RegisterDefaultMessageReceived(SerialCode.ActionCancelPrint, CancelPrintActionCommandAsync);
        await SerialMonitor.SendAsync(new GCodeMessage(GCode.StartPrintJobTimer));
        for(int i = 0; i< 10; i++) {
            await AppendGCodeAsync();
        }
    }

    private async Task PausePrintActionCommandAsync(NewSerialMessageEventArgs args) {
        await PausePrintAsync();
        Console.WriteLine("Print paused");
    }

    private async Task CancelPrintActionCommandAsync(NewSerialMessageEventArgs args) {
        await CancelPrintAsync();
        Console.WriteLine("Print canceled");
    }

    public async Task PausePrintAsync() {
        State = PrintState.Paused;
        SerialMonitor.RemoveDefaultMessageReceived(AppendGCodeAsync);
        PrintStateChanged?.Invoke(this, new PrintEventArgs(State, GetPrintingProgress(), CurrentGCodeLine));

        _beforePauseTemperatures = _printer.State.Temperatures;
        _beforePauseFanSpeed = (int)(_printer.State.FanSpeed * 255);
        State = PrintState.None;
        await _printer.MoveByAsync(z: 10, extrude:-10);
        await _printer.ExcecuteGcodeAsync(new GCodeMessage(GCode.PausePrintJobTimer));
        await _printer.SetTemperatuesAsync(0, 0);
        await _printer.ExcecuteGcode(GCode.SetFanSpeed, "S0");
        State = PrintState.Paused;
    }

    public async Task ResumePrintAsync() {
        State = PrintState.None;
        PrintStateChanged?.Invoke(this, new PrintEventArgs(PrintState.Resumed, GetPrintingProgress(), CurrentGCodeLine));
        await _printer.SetTemperatuesAndWaitAsync(_beforePauseTemperatures.HotEnd.Target, _beforePauseTemperatures.Bed.Target);
        await _printer.MoveByAsync(z: -10, extrude: 10);
        await _printer.ExcecuteGcodeAsync(new GCodeMessage(GCode.StartPrintJobTimer));
        await _printer.ExcecuteGcode(GCode.SetFanSpeed , $"S{_beforePauseFanSpeed}");
        State = PrintState.Printing;

        SerialMonitor.RegisterDefaultMessageReceived(SerialCode.Ok, AppendGCodeAsync);
        await AppendGCodeAsync();
    }

    public async Task CancelPrintAsync() {
        State = PrintState.None;
        await _printer.MoveByAsync(extrude: -2);
        await _printer.MoveByAsync(z: 0.2, extrude: -2);
        await _printer.MoveByAsync(x: 5, y: 5);
        await _printer.MoveByAsync(z: 10);

        // turn everything off
        await _printer.ExcecuteGcodeAsync(new GCodeMessage(GCode.SetFanSpeed, "0"));
        await _printer.ExcecuteGcodeAsync(new GCodeMessage(GCode.SetHotendTemperature, "S0"));
        await _printer.ExcecuteGcodeAsync(new GCodeMessage(GCode.SetBedTemperature, "S0"));

        // disable all steppers but Z
        await _printer.ExcecuteGcodeAsync(new GCodeMessage(GCode.DisableSteppers, "X", "Y", "E"));

        EndPrint();
        PrintStateChanged?.Invoke(this, new PrintEventArgs(PrintState.Canceled, GetPrintingProgress(), CurrentGCodeLine));
    }

    private void EndPrint() {
        State = PrintState.None;
        SerialMonitor.RemoveDefaultMessageReceived(AppendGCodeAsync);
        SerialMonitor.RemoveDefaultMessageReceived(PausePrintActionCommandAsync);
        SerialMonitor.RemoveDefaultMessageReceived(CancelPrintActionCommandAsync);
        PrintStateChanged?.Invoke(this, new PrintEventArgs(State, GetPrintingProgress(), CurrentGCodeLine));
    }

    public double GetPrintingProgress() => (double)CurrentGCodeLine / _gCode.LongLength;

    private Task AppendGCodeAsync(NewSerialMessageEventArgs args) => AppendGCodeAsync();

    private async Task AppendGCodeAsync() {
        if (State == PrintState.Paused || _gCode == null)
            return;

        if (DateTimeGetter.Now - _lastAnalyzed > TimeSpan.FromMinutes(5))
            AnalyzePrintTime();
        await SerialMonitor.SendAsync(new GCodeMessage(_gCode[CurrentGCodeLine]));
        CurrentGCodeLine++;
    }

    private void AnalyzePrintTime() {
        var analyzer = new GCodeAnalyzer(_gCode, CurrentGCodeLine);
        TotalTime = analyzer.Analyze();
        _lastAnalyzed = DateTimeGetter.Now;
    }

    public async ValueTask DisposeAsync() {
        if(State != PrintState.None)
            await CancelPrintAsync();
        _gCode = null;
    }
}

public enum PrintState {
    None,
    Paused,
    Printing,

    Canceled,
    Finished,
    Resumed,
    Started,
}