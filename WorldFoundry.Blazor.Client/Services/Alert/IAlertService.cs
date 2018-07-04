using System;
using System.Net;

namespace WorldFoundry.Blazor.Client.Services
{
    public interface IAlertService
    {
        event EventHandler<AlertEventArgs> Error;
        event EventHandler<AlertEventArgs> Info;
        event EventHandler<AlertEventArgs> Success;
        event EventHandler<AlertEventArgs> Warning;

        void ShowError(string message);
        void ShowInfo(string message);
        void ShowResponseError(HttpStatusCode status, string reason);
        void ShowResponseError(ResponseException ex);
        void ShowSuccess(string message);
        void ShowWarning(string message);
    }
}
