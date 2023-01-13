namespace _3DPrint.NET.Extensions;

public interface IExtensionComponent {
    public string Title { get; }
    public Location Location { get; }
}

public enum Location{
    None,
    Sidebar,
    Main,
    Header
}