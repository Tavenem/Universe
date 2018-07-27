using Microsoft.AspNetCore.Blazor.Browser.Rendering;
using Microsoft.AspNetCore.Blazor.Browser.Services;
using Microsoft.Extensions.DependencyInjection;
using WorldFoundry.Blazor.Client.Services;

namespace WorldFoundry.Blazor.Client
{
#pragma warning disable RCS1102, RCS1018, RCS1163, RCS1021
    public class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = new BrowserServiceProvider(services =>
            {
                services.AddSingleton<IAlertService, AlertService>();
            });

            new BrowserRenderer(serviceProvider).AddComponent<App>("app");
        }
    }
#pragma warning restore RCS1102, RCS1018, RCS1163, RCS1021
}
