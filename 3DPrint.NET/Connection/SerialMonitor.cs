using System.Data;
using System.Diagnostics.Contracts;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.CompilerServices;
using System.Security;

using _3DPrint.NET.Data;
using _3DPrint.NET.Data.EventArguments;
using _3DPrint.NET.Services;

namespace _3DPrint.NET.Connection;
public static class SerialMonitor
{
    public static ConnectionState ConnectionState { get; private set; } = ConnectionState.NotConnected;
    public static string? CurrentSerialPort => SerialWorker.Current.PortName;

    private static List<SerialMessageTaskContainer<NewSerialMessageEventArgs, SerialCode>> s_newSerialMessageReceivedActions = new();
    private static List<SerialMessageTaskContainer<NewSerialMessageEventArgs, SerialCode>> s_newSerialMessageReceivedActionsDefaults = new();

    private static List<SerialMessageTaskContainer<NewGCodeMessageEventArgs, GCode>> s_newSerialMessageSendActions = new();

    private static Queue<GCodeMessage> s_serialMessageBuffer = new();
    private static bool s_okIsAwaited = false;

    private static DateTime _lastResponse;

    public static int[] GetBaudRates() => new int[] {
        300,
        1200,
        2400,
        4800,
        9600,
        19200,
        38400,
        57600,
        74880,
        115200,
        230400,
        250000,
        1000000,
        2000000,
    };

    public static int CurrentBaudRate => SerialWorker.Current.BaudRate;

    public static void RegisterNewMessageReceivedTask(SerialCode code, Func<NewSerialMessageEventArgs, Task> task)
    {
        s_newSerialMessageReceivedActions.Add(new SerialMessageTaskContainer<NewSerialMessageEventArgs, SerialCode>(task, code));
    }

    public static void RegisterDefaultMessageReceived(SerialCode code, Func<NewSerialMessageEventArgs, Task> task)
    {
        s_newSerialMessageReceivedActionsDefaults.Add(new SerialMessageTaskContainer<NewSerialMessageEventArgs, SerialCode>(task, code));
    }

    public static void RegisterNewMessageSendTask(GCode code, Func<NewGCodeMessageEventArgs, Task> task)
    {
        s_newSerialMessageSendActions.Add(new SerialMessageTaskContainer<NewGCodeMessageEventArgs, GCode>(task, code));
    }


    public static void RemoveNewMessageReceivedTask(Func<NewSerialMessageEventArgs, Task> task)
    {
        s_newSerialMessageReceivedActions.RemoveAll(x => x.Func == task);
    }
    public static void RemoveDefaultMessageReceived(Func<NewSerialMessageEventArgs, Task> task)
    {
        s_newSerialMessageReceivedActionsDefaults.RemoveAll(x => x.Func == task);
    }
    public static void RemoveNewMessageSendTask(Func<NewGCodeMessageEventArgs, Task> task)
    {
        s_newSerialMessageSendActions.RemoveAll(x => x.Func == task);
    }

    internal static async Task NewMessageReceivedAsync(SerialMessage serialMessage)
    {
        _lastResponse = DateTimeGetter.Now;
        var args = new NewSerialMessageEventArgs()
        {
            Message = serialMessage
        };

        if (args.Message.Code == SerialCode.Unknown)
            Console.WriteLine("unknown");

        await RunTasks(args, s_newSerialMessageReceivedActions);

        if (args.DefaultProcessing)
        {
            await RunTasks(args, s_newSerialMessageReceivedActionsDefaults);
        }

        if (serialMessage.Code == SerialCode.Ok)
        {
            s_okIsAwaited = false;
            if (s_serialMessageBuffer.Count > 0)
            {
                await SendSerialAsync(s_serialMessageBuffer.Dequeue());
            }
        }

    }

    public static async Task SendAsync(GCodeMessage gCode)
    {
        if (!s_okIsAwaited)
            await SendSerialAsync(gCode);
        else
            s_serialMessageBuffer.Enqueue(gCode);
    }

    public static async Task<SerialMessage[]> SendAndWaitForResponseAsync(GCodeMessage gCode, int timeout = 2000)
    {
        var responseReceiver = new ResponseReceiver(gCode, timeout);
        return await responseReceiver.GetResponseAsync();
    }


    public static async Task<string[]> GetSerialPortNamesAsync() => await SerialWorker.Current.GetSerialPortsAsync();

    public static Task ChangeSerialPortAsync(string portName) => ChangeSerialPortAsync(portName, CurrentBaudRate);
    public static Task ChangeBaudRateAsync(int baudRate) => ChangeSerialPortAsync(CurrentSerialPort, baudRate);

    public static async Task ChangeSerialPortAsync(string portName, int baudRate) {
        _lastResponse = DateTime.MinValue;
        await SerialWorker.Current.SwitchSerialPortAsync(portName, baudRate);
        s_okIsAwaited = false;
        await UpdateConnectionState();
        if (ConnectionState == ConnectionState.Connected)
            await PrinterInitializer.Init();
    }

    public static async Task UpdateConnectionState()
    {
        if (!SerialWorker.Current.IsConnected)
        {
            ConnectionState = ConnectionState.NotConnected;
            return;
        }

        if (DateTimeGetter.Now - _lastResponse < TimeSpan.FromSeconds(10)) {
            ConnectionState = ConnectionState.Connected;
            return;
        }

        ConnectionState = ConnectionState.Loading;

        SerialMessage[] responses = await SendAndWaitForResponseAsync(new GCodeMessage(GCode.SerialPrint, "ping"), 5000);

        ConnectionState = responses.Length == 0 || responses[1].Full != "ping"
            ? ConnectionState.NoResponse
            : ConnectionState.Connected;
    }


    private static async Task SendSerialAsync(GCodeMessage messageToSend)
    {
        if (messageToSend == null)
            Console.WriteLine("???");
        var args = new NewGCodeMessageEventArgs()
        {
            Message = messageToSend
        };
        await RunTasks(args, s_newSerialMessageSendActions);

        if (!args.DefaultProcessing)
            return;

        s_okIsAwaited = true;
        await SerialWorker.Current.Send(messageToSend);
    }

    private static async Task RunTasks<T, Tenum>(T args, List<SerialMessageTaskContainer<T, Tenum>> containers) where T : IMessageEventArgs where Tenum : Enum
    {
        if (args.Message is null)
            return;
        long code;
        do
        {
            code = GetCode(args.Message);
            for (int i = 0; i < containers.Count; i++)
            {
                var container = containers[i];
                if (!IsEqual(code, container.SerialCode))
                    continue;
                await container.Func.Invoke(args);
                if (!args.FurtherProcessing || code != GetCode(args.Message))
                    break;
            }
        } while (args.FurtherProcessing && code != GetCode(args.Message));
    }

    private static long GetCode(SerialMessage message) => message is GCodeMessage msg ? (long)msg.GCode : (long)message.Code;

    private static bool IsEqual<TEnum>(long code, TEnum toCompare) where TEnum : Enum =>
        toCompare is GCode gCode
        ? code == gCode.ToLong()
        : (toCompare.ToLong() & code) == code;
}

public enum ConnectionState
{
    NotConnected,
    Connected,
    Error,
    NoResponse,
    Loading
}