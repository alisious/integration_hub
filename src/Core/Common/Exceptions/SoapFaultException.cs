using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.Common.Exceptions
{
    public sealed class SoapFaultException : Exception
    {
        public string Endpoint { get; }
        public string Action { get; }
        public string? RequestId { get; }
        public string? FaultCode { get; }

        public SoapFaultException(string faultMessage, string endpoint, string action,
                                string? requestId = null, string? faultCode = null)
            : base(faultMessage)
        {
            Endpoint = endpoint;
            Action = action;
            RequestId = requestId;
            FaultCode = faultCode;
        }
    }
}
