using System.Runtime.Versioning;
using System.Threading.Tasks;
using Atlas.UI.Avalonia.Examples;
using Avalonia;
using Avalonia.Browser;
//using Avalonia.ReactiveUI;
//using Atlas.Browser;

[assembly: SupportedOSPlatform("browser")]

internal sealed partial class Program
{
    private static Task Main(string[] args) => BuildAvaloniaApp()
            .WithInterFont()
           // .UseReactiveUI()
            .StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}
