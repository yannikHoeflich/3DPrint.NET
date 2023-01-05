using System.Threading;
using _3DPrint.NET.Connection;
using _3DPrint.NET.Data;
using _3DPrint.NET.Data.EventArguments;

namespace _3DPrint.NET.Services;
internal class ResponseReceiver {
    private readonly List<SerialMessage> _response = new List<SerialMessage>();
    private bool _receivedOk = false;
    private readonly GCodeMessage _message;
    private readonly int _timeout;

    public ResponseReceiver(GCodeMessage message, int timeout) {
        _message = message;
        _timeout = timeout;
    }

    public async Task<SerialMessage[]> GetResponseAsync() {
        SerialMonitor.RegisterNewMessageSendTask(_message.GCode, MessageSend);
        await SerialMonitor.SendAsync(_message);

        while (!_receivedOk)
            await Task.Delay(10);

        return _response.ToArray();
    }

    private Task MessageSend(NewGCodeMessageEventArgs args) {
        if (args.Message != _message)
            return Task.CompletedTask;


        SerialMonitor.RegisterDefaultMessageReceived(SerialCode.All, MessageReceived);
        SerialMonitor.RemoveNewMessageSendTask(MessageSend);
        Task.Run(async () => {
            await Task.Delay(_timeout);
            End();
        });
        return Task.CompletedTask;
    }

    private Task MessageReceived(NewSerialMessageEventArgs args) {
        _response.Add(args.Message);

        if (args.Message.Code == SerialCode.Ok) {
            End();
            return Task.CompletedTask;
        }
        return Task.CompletedTask;
    }

    private void End() {
        _receivedOk = true;
        SerialMonitor.RemoveDefaultMessageReceived(MessageReceived);
    }
}
