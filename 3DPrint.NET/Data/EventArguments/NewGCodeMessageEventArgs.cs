namespace _3DPrint.NET.Data.EventArguments;

public class NewGCodeMessageEventArgs : IMessageEventArgs
{
    public GCodeMessage Message { get; init; }
    public bool FurtherProcessing { get; set; } = true;
    public bool DefaultProcessing { get; set; } = true;
    SerialMessage IMessageEventArgs.Message => Message;

    public void OverrideMessage(GCode gCode, params string[] args) => Message.Override(gCode, args);
    void IMessageEventArgs.OverrideMessage(string message) => Message.Override(message, Message.Sender);
}
