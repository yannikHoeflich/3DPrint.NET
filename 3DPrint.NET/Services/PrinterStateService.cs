using System.Globalization;
using System.Runtime.CompilerServices;

using _3DPrint.NET.Connection;
using _3DPrint.NET.Data;
using _3DPrint.NET.Data.EventArguments;
using _3DPrint.NET.Data.Processors;
using _3DPrint.NET.Data.Values;

namespace _3DPrint.NET.Services;
public class PrinterStateService {
    public static PrinterStateService Current { get; private set; }
    public TemperatureContainer Temperatures { get; private set; }
    public Coordinate Position { get; internal set; }
    public Cube Size { get; internal set; }
    public List<string> ActionNotifications { get; } = new List<string>();

    public ConnectionState ConnectionState => SerialMonitor.ConnectionState;
    public double FanSpeed { get; private set; }

    internal async Task InitAsync() {
        Current = this;
        SerialMonitor.RegisterDefaultMessageReceived(SerialCode.Temperature, ReadTemp);
        SerialMonitor.RegisterDefaultMessageReceived(SerialCode.ActionNotification, PrintNotification);
        SerialMonitor.RegisterDefaultMessageReceived(SerialCode.PositionReport, ReadPositionReport);
        SerialMonitor.RegisterNewMessageSendTask(GCode.LinearMove, ReadPositionGCode);
        SerialMonitor.RegisterNewMessageSendTask(GCode.LinearMoveNotPrinting, ReadPositionGCode);

        SerialMonitor.RegisterNewMessageSendTask(GCode.SetHotendTemperature, ReadHotEndGCode);
        SerialMonitor.RegisterNewMessageSendTask(GCode.SetBedTemperature, ReadBedGCode);

        SerialMonitor.RegisterNewMessageSendTask(GCode.SetFanSpeed, ReadFanSpeedGCode);
        SerialMonitor.RegisterNewMessageSendTask(GCode.FanOff, x => {
            FanSpeed = 0;
            return Task.CompletedTask;
        });
    }

    private Task ReadFanSpeedGCode(NewGCodeMessageEventArgs arg) {
        var values = ResponseParser.ParseGcode(arg.Message.Arguments);

        if(!values.ContainsKey('S'))
            return Task.CompletedTask;

        FanSpeed = values['S'] / 255;

        return Task.CompletedTask;
    }

    private Task ReadHotEndGCode(NewGCodeMessageEventArgs arg) {
        double temp = ResponseParser.ParseTemperatureRequest(arg.Message);
        Temperatures = new TemperatureContainer(new Temperature(temp, Temperatures.HotEnd.Current), Temperatures.Bed);
        return Task.CompletedTask;
    }

    private Task ReadBedGCode(NewGCodeMessageEventArgs arg) {
        double temp = ResponseParser.ParseTemperatureRequest(arg.Message);
        Temperatures = new TemperatureContainer(Temperatures.HotEnd, new Temperature(temp, Temperatures.Bed.Current));
        return Task.CompletedTask;
    }

    public Coordinate GetSize() {
        return new Coordinate(
            Size.Max.X - Size.Min.X,
            Size.Max.Y - Size.Min.Y,
            Size.Max.Z - Size.Min.Z
            );
    }

    private Task ReadPositionGCode(NewGCodeMessageEventArgs arg) {
        string[] message = arg.Message.Arguments;

        if (message.Length == 0)
            return Task.CompletedTask;

        (double? x, double? y, double? z, double? e) = ResponseParser.ParseCoordinates(message);

        Position = new Coordinate(
            x ?? Position.X,
            y ?? Position.Y,
            z ?? Position.Z,
            e ?? Position.E
            );

        return Task.CompletedTask;
    }


    private Task ReadPositionReport(NewSerialMessageEventArgs arg) {
        double? x = null;
        double? y = null;
        double? z = null;
        double? e = null;

        foreach (var part in arg.Message.Full.Split(' ')) {
            if (part == "Count")
                break;

            var splitted = part.Split(':');
            if (splitted.Length < 2)
                continue;
            string axis = splitted[0];
            if (!double.TryParse(splitted[1], CultureInfo.InvariantCulture, out double value))
                continue;

            switch (axis) {
                case "X":
                    x = value;
                    break;
                case "Y":
                    y = value;
                    break;

                case "Z":
                    z = value;
                    break;

                case "E":
                    z = value;
                    break;
            }
        }

        Position = new Coordinate(
            x ?? Position.X,
            y ?? Position.Y,
            z ?? Position.Z,
            e ?? Position.E
            );

        return Task.CompletedTask;
    }

    private async Task PrintNotification(NewSerialMessageEventArgs arg) {
        string message = string.Join(' ', arg.Message.Full.Split(' ')[1..]);
        ActionNotifications.Insert(0, message);
    }

    private async Task ReadTemp(NewSerialMessageEventArgs args) {
        string[] splitted = args.Message.Full.Split(' ');

        for (int i = 0; i < splitted.Length; i += 2) {
            if (splitted[i].StartsWith('@'))
                break;

            double actualTemp = double.Parse(splitted[i].Split(':')[1], CultureInfo.InvariantCulture);
            double targetTemp = double.Parse(splitted[i + 1].Trim('/'), CultureInfo.InvariantCulture);

            var temp = new Temperature(targetTemp, actualTemp);

            switch (splitted[i][0]) {
                case 'T':
                    Temperatures = new TemperatureContainer(temp, Temperatures.Bed);
                    break;
                case 'B':
                    Temperatures = new TemperatureContainer(Temperatures.HotEnd, temp);
                    break;
            }
        }
    }

}
