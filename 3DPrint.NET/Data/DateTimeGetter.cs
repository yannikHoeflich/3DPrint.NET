namespace _3DPrint.NET.Data;

public static class DateTimeGetter {
    private static int s_startTicks;
    private static DateTime s_startDateTime;

    static DateTimeGetter() {
        s_startTicks = Environment.TickCount;
        s_startDateTime = DateTime.Now;
    }

    public static DateTime Now {
        get {
            return s_startDateTime.AddMilliseconds(Environment.TickCount - s_startTicks);
        }
    }
}
