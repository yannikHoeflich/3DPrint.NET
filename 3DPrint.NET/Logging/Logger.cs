using System.Xml.Linq;

namespace _3DPrint.NET.Logging;
public class Logger : ILogger {
    private string _name;

    private static object _lock = new object();

    public Logger(string name) {
        _name = name;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

    public bool IsEnabled(LogLevel logLevel) {
#if DEBUG
        return true;
#else
        return logLevel >= LogLevel.Information;
#endif
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
        if (!IsEnabled(logLevel))
            return;

        lock (_lock) {
            Console.Write($"[{DateTime.Now:G}, {_name}] ");
            Console.ForegroundColor = GetColor(logLevel);
            Console.Write($"{logLevel}: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"{formatter(state, exception)}");
        }
    }

    public static ILogger<T> Create<T>() => Program.GetLogger<T>();

    private ConsoleColor GetColor(LogLevel logLevel) => logLevel switch {
        LogLevel.Trace => ConsoleColor.Gray,
        LogLevel.Debug => ConsoleColor.White,
        LogLevel.Information => ConsoleColor.Green,
        LogLevel.Warning => ConsoleColor.Yellow,
        LogLevel.Error => ConsoleColor.Red,
        LogLevel.Critical => ConsoleColor.Red,
        LogLevel.None => ConsoleColor.White,
        _ => ConsoleColor.White
    };
}
