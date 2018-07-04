using System;
using System.Net;

namespace WorldFoundry.Blazor.Client
{
    public class ResponseException : Exception
    {
        public HttpStatusCode StatusCode { get; set; }
        public string Reason { get; set; }
    }
}
