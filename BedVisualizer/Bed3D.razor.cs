using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using _3DPrint.NET.Extensions;
using System;

namespace BedVisualizer;
public partial class Bed3D : IExtensionComponent {
    public string Title { get; } = "Bed Visualizer";

    public Location Location { get; } = Location.Main;
}
