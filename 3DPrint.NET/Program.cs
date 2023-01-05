using _3DPrint.NET.Connection;
using _3DPrint.NET.Data;
using _3DPrint.NET.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace _3DPrint.NET;
public static class Program {
    public static async Task Main(string[] args) {
        var serialWorker = new SerialWorker("COM4");
        var serialWorkerThread = new Thread(async () => await serialWorker.Start());
        serialWorkerThread.Start();

        var builder = WebApplication.CreateBuilder(args);

        AddServices(builder.Services);

        var app = builder.Build();

        await InitServices(app.Services);

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment()) {
            app.UseExceptionHandler("/Error");
        }


        app.UseStaticFiles();

        app.UseRouting();

        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

        app.Run();
    }

    private static void AddServices(IServiceCollection services) {
        services.AddRazorPages();
        services.AddServerSideBlazor();

        services.AddSingleton<PrinterStateService>();
        services.AddSingleton<Printer>();
    }

    private static async Task InitServices(IServiceProvider services) {
        await services.GetService<PrinterStateService>().InitAsync();
        await services.GetService<Printer>().InitAsync();
    }
}
