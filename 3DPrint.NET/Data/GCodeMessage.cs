namespace _3DPrint.NET.Data;

public class GCodeMessage : SerialMessage {
    public GCode GCode { get; private set;}
    public string[] Arguments { get; private set; }

    public GCodeMessage(string message) : base(message) {
        string[] splitted = message.Split(' ');
        GCode code = GCodes.Parse(splitted[0]);
        GCode = code;
        Arguments = splitted[1..];
    }

    public GCodeMessage(GCode gCode, params string[] args) : base(CreateGCode(gCode, args)) {
        GCode = gCode;
        Arguments = args;
    }

    internal void Override(GCode gCode, params string[] args) {
        GCode = gCode;
        Arguments = args;
        base.Override(CreateGCode(GCode, Arguments), this.Sender);
    }

    internal override void Override(string message, Sender senderSender) {
        var splitted = message.Split(' ');
        var code = GCodes.Parse(splitted[0]);
        Override(code, splitted[1..]);
    }

    private static string CreateGCode(GCode gCode, string[] args) {

        return $"{gCode.ToGCode()} {string.Join(' ', args)}";
    }
}