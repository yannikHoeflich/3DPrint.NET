using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using _3DPrint.NET.Extensions;
using Microsoft.Extensions.Logging;
using System;

namespace TestExtension;
public class TestBackground : IExtension {
    private ILogger<TestBackground> _logger;
    public Task InitAsync() {
        _logger = _3DPrint.NET.Logging.Logger.Create<TestBackground>();

        _logger.LogInformation("Init");
        return Task.CompletedTask;
    }
    public Task RunAsync() {
        _logger.LogInformation("Run");

        return Task.CompletedTask;
    }
}
