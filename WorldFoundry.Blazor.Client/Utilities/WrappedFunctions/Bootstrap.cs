using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace WorldFoundry.Blazor.Client
{
    public static class Bootstrap
    {
        public static Task<bool> CollapseHide(string selector) => JSRuntime.Current.InvokeAsync<bool>("bootstrapBlazorFunctions.bsCollapseHide", selector);
        public static Task<bool> CollapseShow(string selector) => JSRuntime.Current.InvokeAsync<bool>("bootstrapBlazorFunctions.bsCollapseShow", selector);
        public static Task<bool> CollapseShowTimed(string selector, int timeout) => JSRuntime.Current.InvokeAsync<bool>("bootstrapBlazorFunctions.bsCollapseShowTimed", selector, timeout);
        public static Task<bool> EnableTooltips() => JSRuntime.Current.InvokeAsync<bool>("bootstrapBlazorFunctions.bsEnableTooltips");
        public static Task<bool> ModalShow(string selector) => JSRuntime.Current.InvokeAsync<bool>("bootstrapBlazorFunctions.bsModalShow", selector);
    }
}
