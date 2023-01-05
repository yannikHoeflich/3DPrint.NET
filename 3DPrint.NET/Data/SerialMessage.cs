namespace _3DPrint.NET.Data;

public class SerialMessage {
    public string Full { get; private set; }
    public Sender Sender { get; private set; }
    public SerialCode Code { get; private set; }


    internal SerialMessage(string message, Sender senderSender) {
        Sender = senderSender;
        ParseMessage(message);
    }

    public SerialMessage(string message) : this(message, Sender.Host) {}

    internal virtual void Override(string message, Sender senderSender) {
        Sender = senderSender;
        ParseMessage(message);
    }

    private void ParseMessage(string message) {
        Code = SerialCodeExtensions.Parse(message);
        Full = message;
    }
}

public enum Sender {
    Host,
    Client
}