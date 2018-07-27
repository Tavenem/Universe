using System;

namespace WorldFoundry.Blazor.Client.Services
{
    public class LoadingBarService : ILoadingBarService
    {
        public event EventHandler<MessageEventArgs> Loading;
        public event EventHandler<EventArgs> DoneLoading;

        public void ShowLoading(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                OnLoading(new MessageEventArgs { Message = "Loading..." });
            }
            else
            {
                OnLoading(new MessageEventArgs { Message = message });
            }
        }

        public void HideLoading() => OnDoneLoading(EventArgs.Empty);

        protected virtual void OnLoading(MessageEventArgs e) => Loading?.Invoke(this, e);
        protected virtual void OnDoneLoading(EventArgs e) => DoneLoading?.Invoke(this, e);
    }
}
