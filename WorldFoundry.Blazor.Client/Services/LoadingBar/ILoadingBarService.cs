using System;

namespace WorldFoundry.Blazor.Client.Services
{
    public interface ILoadingBarService
    {
        event EventHandler<EventArgs> DoneLoading;
        event EventHandler<MessageEventArgs> Loading;

        void HideLoading();
        void ShowLoading(string message);
    }
}
