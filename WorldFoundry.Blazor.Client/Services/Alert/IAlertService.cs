using System;
using System.Net;

namespace WorldFoundry.Blazor.Client.Services
{
    public interface IAlertService
    {
        event EventHandler<MessageEventArgs> Error;
        event EventHandler<MessageEventArgs> Info;
        event EventHandler<MessageEventArgs> Success;
        event EventHandler<MessageEventArgs> Warning;

        void ShowError(string message);
        void ShowInfo(string message);
        void ShowResponseError(HttpStatusCode status, string reason);
        void ShowResponseError(ResponseException ex);
        void ShowSuccess(string message);
        void ShowWarning(string message);
    }
}
