using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace _3DPrint.NET.Extensions.Internal;
internal class DefaultExtension : IExtension {
    public Task InitAsync() => Task.CompletedTask;
    public Task RunAsync() => Task.CompletedTask;
}
