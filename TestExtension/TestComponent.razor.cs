using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using _3DPrint.NET.Extensions;
using System;

namespace TestExtension;
public partial class TestComponent : IExtensionComponent {
    public string Title {get;} = "Test";

    public Location Location { get; } = Location.Main;
}
