using System;
using System.Net;

namespace WorldFoundry.Blazor.Client.Services
{
    public class AlertService : IAlertService
    {
        public event EventHandler<MessageEventArgs> Error;
        public event EventHandler<MessageEventArgs> Info;
        public event EventHandler<MessageEventArgs> Success;
        public event EventHandler<MessageEventArgs> Warning;

        public void ShowError(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                OnError(new MessageEventArgs { Message = message });
            }
        }

        public void ShowInfo(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                OnInfo(new MessageEventArgs { Message = message });
            }
        }

        public void ShowResponseError(HttpStatusCode status, string reason)
        {
            if (!string.IsNullOrWhiteSpace(reason))
            {
                OnError(new MessageEventArgs { Message = $"{(int)status} {status} - {reason}" });
            }
        }

        public void ShowResponseError(ResponseException ex)
            => ShowResponseError(ex.StatusCode, ex.Reason);

        public void ShowSuccess(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                OnSuccess(new MessageEventArgs { Message = message });
            }
        }

        public void ShowWarning(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                OnWarning(new MessageEventArgs { Message = message });
            }
        }

        protected virtual void OnError(MessageEventArgs e) => Error?.Invoke(this, e);
        protected virtual void OnInfo(MessageEventArgs e) => Error?.Invoke(this, e);
        protected virtual void OnSuccess(MessageEventArgs e) => Error?.Invoke(this, e);
        protected virtual void OnWarning(MessageEventArgs e) => Error?.Invoke(this, e);
    }
}
