namespace _3DPrint.NET.Saving;

public class MainConfig {
    public SerialPortData? SerialPort { get; set; }


}

public class SerialPortData {
    public string Port { get; set; }
    public int BaudRate { get; set; }
}