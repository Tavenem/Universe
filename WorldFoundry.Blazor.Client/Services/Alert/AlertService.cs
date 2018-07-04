using System;
using System.Net;

namespace WorldFoundry.Blazor.Client.Services
{
    public class AlertService : IAlertService
    {
        public event EventHandler<AlertEventArgs> Error;
        public event EventHandler<AlertEventArgs> Info;
        public event EventHandler<AlertEventArgs> Success;
        public event EventHandler<AlertEventArgs> Warning;

        public void ShowError(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                OnError(new AlertEventArgs { Message = message });
            }
        }

        public void ShowInfo(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                OnInfo(new AlertEventArgs { Message = message });
            }
        }

        public void ShowResponseError(HttpStatusCode status, string reason)
        {
            if (!string.IsNullOrWhiteSpace(reason))
            {
                OnError(new AlertEventArgs { Message = $"{(int)status} {status} - {reason}" });
            }
        }

        public void ShowResponseError(ResponseException ex)
            => ShowResponseError(ex.StatusCode, ex.Reason);

        public void ShowSuccess(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                OnSuccess(new AlertEventArgs { Message = message });
            }
        }

        public void ShowWarning(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                OnWarning(new AlertEventArgs { Message = message });
            }
        }

        protected virtual void OnError(AlertEventArgs e) => Error?.Invoke(this, e);
        protected virtual void OnInfo(AlertEventArgs e) => Error?.Invoke(this, e);
        protected virtual void OnSuccess(AlertEventArgs e) => Error?.Invoke(this, e);
        protected virtual void OnWarning(AlertEventArgs e) => Error?.Invoke(this, e);
    }
}
