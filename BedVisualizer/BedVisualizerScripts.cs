using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using _3DPrint.NET.Extensions;
using System;

namespace BedVisualizer;
public class BedVisualizerScripts : IExtensionScript {
    public string[] ScriptUrls { get; } = new string[] {
        "_content/Plotly.Blazor/plotly-latest.min.js",
        "_content/Plotly.Blazor/plotly-interop.js",
    };
}
