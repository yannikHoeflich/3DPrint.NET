using _3DPrint.NET.Extensions;

namespace _3DPrint.NET.Shared.DefaultExtensionComponents.Main;
public partial class PrinterController : IExtensionComponent {
    public string Title { get; } = "Controlls";

    public Location Location { get; } = Location.Main;
}
