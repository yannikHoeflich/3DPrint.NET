namespace _3DPrint.NET.Data;

public static class DateTimeGetter {
    private static int s_startTicks;
    private static DateTime s_startDateTime;

    static DateTimeGetter() {
        Sync();
    }

    public static DateTime Now => s_startDateTime.AddMilliseconds(Environment.TickCount - s_startTicks);

    public static Task SyncAsync() => Task.Run(Sync);
    private static void Sync() { 
        s_startTicks = Environment.TickCount;
        s_startDateTime = DateTime.Now;
    }
}
