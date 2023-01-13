namespace _3DPrint.NET.Extensions;

public interface IExtension {
    public Task InitAsync();
    public Task RunAsync();
}
