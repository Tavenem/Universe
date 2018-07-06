using Microsoft.JSInterop;
using WorldFoundry.Blazor.Client.Services;

namespace WorldFoundry.Blazor.Client.JSInterop
{
    public class Alerts
    {
        private static IAlertService _alert;

        public Alerts(IAlertService alertService) => _alert = alertService;

        [JSInvokable("ShowError")]
        public static void ShowError(string message) => _alert.ShowError(message);

        [JSInvokable("ShowInfo")]
        public static void ShowInfo(string message) => _alert.ShowInfo(message);

        [JSInvokable("ShowSuccess")]
        public static void ShowSuccess(string message) => _alert.ShowSuccess(message);

        [JSInvokable("ShowWarning")]
        public static void ShowWarning(string message) => _alert.ShowWarning(message);
    }
}
