using System;
using System.Net;

namespace WorldFoundry.Blazor.Client
{
    public class ResponseException : Exception
    {
        public HttpStatusCode StatusCode { get; set; }

        public string Reason { get; set; }

        public ResponseException() { }

        protected ResponseException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

        public ResponseException(string message) : base(message) { }

        public ResponseException(string message, Exception innerException) : base(message, innerException) { }
    }
}
