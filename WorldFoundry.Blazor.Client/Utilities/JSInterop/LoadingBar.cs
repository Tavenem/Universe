using Microsoft.JSInterop;
using WorldFoundry.Blazor.Client.Services;

namespace WorldFoundry.Blazor.Client.JSInterop
{
    public class LoadingBar
    {
        private static ILoadingBarService _loadingBar;

        public LoadingBar(ILoadingBarService loadingBarService) => _loadingBar = loadingBarService;

        [JSInvokable("ShowLoading")]
        public static void ShowLoading(string message) => _loadingBar.ShowLoading(message);

        [JSInvokable("HideLoading")]
        public static void HideLoading() => _loadingBar.HideLoading();
    }
}
