namespace _3DPrint.NET.Data.EventArguments;

public class NewSerialMessageEventArgs : IMessageEventArgs
{
    public SerialMessage Message { get; init; }
    public bool FurtherProcessing { get; set; } = true;
    public bool DefaultProcessing { get; set; } = true;

    public void OverrideMessage(string message) => Message.Override(message, Message.Sender);

    public void PreventFurtherProcessing()
    {
        FurtherProcessing = false;
        PreventDefaultProccessing();
    }

    public void PreventDefaultProccessing() => DefaultProcessing = true;
}
