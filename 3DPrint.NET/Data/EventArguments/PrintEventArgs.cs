using _3DPrint.NET.Services;

namespace _3DPrint.NET.Data.EventArguments;
public class PrintEventArgs : EventArgs{
    public PrintEventArgs(PrintState printState, double progress, long line) {
        PrintState = printState;
        Progress = progress;
        Line = line;
    }

    public PrintState PrintState { get; init; }
    public double Progress { get; init; }
    public long Line { get; init; }
}
