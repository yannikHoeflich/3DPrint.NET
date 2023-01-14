using _3DPrint.NET.Extensions;

namespace _3DPrint.NET.Shared.DefaultExtensionComponents.Sidebar;
public partial class ActionNotifications : IExtensionComponent{
    public string Title { get; } = "Action Notifications";

    public Location Location { get; } = Location.Sidebar;
}