using System.Reflection;

using _3DPrint.NET.Connection;
using _3DPrint.NET.Data;
using _3DPrint.NET.Extensions;
using _3DPrint.NET.Extensions.Internal;
using _3DPrint.NET.Logging;
using _3DPrint.NET.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.CodeAnalysis;
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
        ICollection<IExtension> extensions = await LoadExtensionsAsync<IExtension>(logger, () => new DefaultExtension());
        ICollection<IExtensionScript> scripts = await LoadExtensionsAsync<IExtensionScript>(logger, () => new DefaultExtensionScript());
        logger.LogInformation("loading Extensions done!");
        logger.LogInformation("{} Extensions found", extensions.Count);

        logger.LogInformation($"initializing extensions . . .");
        await InitExtensionsAsync(extensions, logger);
        logger.LogInformation($"done initializing extensions");

        logger.LogInformation($"starting extensions . . .");
        await StartExtensionsAsync(extensions, logger);
        logger.LogInformation($"done loading extensions!");

        logger.LogInformation($"loading extension scripts . . .");
        LoadExtensionScripts(scripts, app, logger);
        logger.LogInformation($"done loading extension scripts!");

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

    private static void LoadExtensionScripts(ICollection<IExtensionScript> extensions, WebApplication app, ILogger<Program> logger) {
        var scriptService = app.Services.GetService<ScriptService>();

        foreach (var urls in extensions) {
            scriptService.AddUrls(urls.ScriptUrls);
        }
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

        services.AddSingleton<SavingService>();
        services.AddSingleton<ScriptService>();

        services.AddPluginFramework()
                .AddPluginCatalog(new AssemblyPluginCatalog(typeof(Program).Assembly))
                .AddPluginCatalog(new FolderPluginCatalog(s_extensionPath))
                .AddPluginType<IExtensionComponent>();
    }

    private static async Task InitServices(IServiceProvider services) {
        await services.GetService<SavingService>().LoadAsync();

        await services.GetService<PrinterStateService>().InitAsync();
        await services.GetService<Printer>().InitAsync();
    }

    private static async Task<ICollection<T>> LoadExtensionsAsync<T>(ILogger<Program> logger, Func<T> createDefault) {
        if (!Directory.Exists(s_extensionPath)) {
            Directory.CreateDirectory(s_extensionPath);
            logger.LogInformation("extension folder created");
        }

        var folderExtensionCatalog = new FolderPluginCatalog(s_extensionPath, type => type.Implements<T>());
        await folderExtensionCatalog.Initialize();
        List<Plugin> assemplyExtensions = folderExtensionCatalog.GetPlugins();

        var assemblyExtensionCatalog = new AssemblyPluginCatalog(Assembly.GetExecutingAssembly(), type => type.Implements<T>());
        await assemblyExtensionCatalog.Initialize();
        assemplyExtensions.AddRange(assemblyExtensionCatalog.GetPlugins());

        return assemplyExtensions.Select(extensionType => (T?)Activator.CreateInstance(extensionType))
                                 .Where(x => x is not null)
                                 .Select(x => x ?? createDefault())
                                 .ToArray();
    }

    private static async Task InitExtensionsAsync(ICollection<IExtension> extensions, ILogger<Program> logger) {
        foreach (IExtension extension in extensions) {
            logger.LogInformation("initializing {}", extension.GetType());
            Task.Run(() => {
                try {
                    extension.InitAsync();
                } catch (Exception ex) {
                    logger.LogError("extension {} hat an error while initializing!", extension.GetType());
                    logger.LogDebug("{}", ex);
                }
            });
            logger.LogInformation("done initializing {}!", extension.GetType());
        }
    }

    private static async Task StartExtensionsAsync(ICollection<IExtension> extensions, ILogger<Program> logger) {
        foreach (IExtension extension in extensions) {
            logger.LogInformation("starting {}", extension.GetType());
            Task.Run(() => {
                try {
                    extension.RunAsync();
                } catch (Exception ex){
                    logger.LogError("extension {} hat error an while running!", extension.GetType());
                    logger.LogDebug("{}", ex);
                }
            });
            logger.LogInformation("done starting {}!", extension.GetType());
        }
    }
}
