using _3DPrint.NET.Connection;
using _3DPrint.NET.Data;
using _3DPrint.NET.Extensions;
using _3DPrint.NET.Extensions.Internal;
using _3DPrint.NET.Logging;
using _3DPrint.NET.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Weikio.PluginFramework.Abstractions;
using Weikio.PluginFramework.Catalogs;

namespace _3DPrint.NET;
internal class Program {
    private const string s_extensionPath = @"./extensions";

    private static WebApplication? s_app;

    public static async Task Main(string[] args) {

        var serialWorker = new SerialWorker("COM6");
        var serialWorkerThread = new Thread(async () => await serialWorker.Start());
        serialWorkerThread.Start();

        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        AddServices(builder.Services);

        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new Provider());

        WebApplication app = builder.Build();
        s_app = app;

        ILogger<Program> logger = GetLogger<Program>();
        logger.LogInformation("Logger started!");
        logger.LogInformation("Already done: ");
        logger.LogInformation("- started Serial Worker");
        logger.LogInformation("- initialize default Services");
        logger.LogInformation("- build Web Application");

        logger.LogInformation("loading Extensions . . .");
        ICollection<IExtension> extensions = await LoadExtensionsAsync(logger);
        logger.LogInformation("loading Extensions done!");
        logger.LogInformation("{} Extensions found", extensions.Count);

        logger.LogInformation($"initializing extensions . . .");
        await InitExtensionsAsync(extensions, logger);
        logger.LogInformation($"done initializing extensions");

        logger.LogInformation($"starting extensions . . .");
        await StartExtensionsAsync(extensions, logger);
        logger.LogInformation($"done loading extensions!");

        logger.LogInformation($"starting Web Application . . .");

        await InitServices(app.Services);

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment()) {
            app.UseExceptionHandler("/Error");
        }

        app.UseStaticFiles();

        app.UseRouting();

        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");
        app.MapControllers();

        app.Run();
    }

    public static ILogger<T> GetLogger<T>() {
        if (s_app is null)
            return null;

        return s_app.Services.GetRequiredService<ILoggerFactory>().CreateLogger<T>();
    }

    private static void AddServices(IServiceCollection services) {
        services.AddRazorPages();
        services.AddServerSideBlazor();

        services.AddSingleton<PrinterStateService>();
        services.AddSingleton<Printer>();

        services.AddPluginFramework<IExtensionComponent>(s_extensionPath);
    }

    private static async Task InitServices(IServiceProvider services) {
        await services.GetService<PrinterStateService>().InitAsync();
        await services.GetService<Printer>().InitAsync();
    }

    private static async Task<ICollection<IExtension>> LoadExtensionsAsync(ILogger<Program> logger) {
        if (!Directory.Exists(s_extensionPath)) {
            Directory.CreateDirectory(s_extensionPath);
            logger.LogInformation("extension folder created");
        }

        var extensionCatalog = new FolderPluginCatalog(s_extensionPath, type => type.Implements<IExtension>());

        await extensionCatalog.Initialize();

        List<Plugin> assemplyExtensions = extensionCatalog.GetPlugins();

        return assemplyExtensions.Select(extensionType => (IExtension?)Activator.CreateInstance(extensionType))
                                 .Where(x => x is not null)
                                 .Select(x => x??new DefaultExtension())
                                 .ToArray();
    }

    private static async Task InitExtensionsAsync(ICollection<IExtension> extensions, ILogger<Program> logger) {
        foreach(IExtension extension in extensions) {
            logger.LogInformation("initializing {}", extension.GetType());
            await extension.InitAsync();
            logger.LogInformation("done initializing {}!", extension.GetType());
        }
    }

    private static async Task StartExtensionsAsync(ICollection<IExtension> extensions, ILogger<Program> logger) {
        foreach (IExtension extension in extensions) {
            logger.LogInformation("starting {}", extension.GetType());
            Task.Run(extension.RunAsync);
            logger.LogInformation("done starting {}!", extension.GetType());
        }
    }
}
