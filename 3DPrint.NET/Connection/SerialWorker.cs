using System.ComponentModel;
using System.Globalization;
using System.IO.Ports;

using _3DPrint.NET.Data;

namespace _3DPrint.NET.Connection;
internal class SerialWorker {
    public static SerialWorker Current { get; private set; }

    public string? PortName { get; private set; }
    public int BaudRate { get; private set; }
    public bool IsConnected => _serialPort != null;

    private bool _continue;
    private SerialPort? _serialPort;

    private const int s_retryCount = 5;


    public SerialWorker(string? portName = null, int?  baudRate = null) {
        var port = new SerialPort();

        string[] portNames = SerialPort.GetPortNames();
        if (portNames.Length == 0)
            return;

        PortName = portNames.Contains(portName) 
                ? portName 
                : portNames.First();

        BaudRate = baudRate ?? 115200;

        ConfigurePort(port);
        _serialPort = port;
    }


    public async Task Start() {
        Current = this;

        if (_serialPort != null) {
            await Task.Run(_serialPort.Open);
        }
        _continue = true;

        Task.Run(async () => {
            await Task.Delay(1000);
            await SerialMonitor.UpdateConnectionState();
        });
        await Read();
    }

    private void ConfigurePort(SerialPort serialPort) {
        serialPort.PortName = PortName;
        serialPort.BaudRate = BaudRate;
        serialPort.Parity = Parity.None;
        serialPort.DataBits = 8;
        serialPort.StopBits = StopBits.One;
        serialPort.Handshake = Handshake.None;

        serialPort.ReadTimeout = 500;
        serialPort.WriteTimeout = 500;
    }

    private async Task Read() {
        while (_continue) {
            while (_serialPort == null)
                await Task.Delay(1);
            try {
                string messageStr = await Task.Run(_serialPort.ReadLine);
                messageStr = messageStr.Trim();
                var message = new SerialMessage(messageStr, Sender.Client);
                await SerialMonitor.NewMessageReceivedAsync(message);
            } catch (IOException ex) {
                if (ex.HResult != -2147023436)
                    throw;
            } catch (TimeoutException) {
                // can ignore because normal when printer doesn't send data for more that 500ms
            } catch(OperationCanceledException) {
                // can ignore because happens when changing serial port
            }
        }
    }

    public async Task Send(SerialMessage message) {
        if (_serialPort == null)
            return;

        int retries = 0;
        while (retries < s_retryCount) {
            try {
                await Task.Run(() => _serialPort.WriteLine(message.Full));
            } catch (TimeoutException) {
                retries++;
                continue;
            }

            break;
        }
    }

    public Task<string[]> GetSerialPortsAsync() => Task.Run(SerialPort.GetPortNames);

    public async Task SwitchSerialPortAsync(string portName, int baudRate) {
        var newPort = new SerialPort();
        PortName = portName;
        BaudRate = baudRate;
        ConfigurePort(newPort);

        if (_serialPort != null && _serialPort.IsOpen) {
            await Task.Run(_serialPort.Close);
        }

        await Task.Run(newPort.Open);

        _serialPort = newPort;
    }
}
