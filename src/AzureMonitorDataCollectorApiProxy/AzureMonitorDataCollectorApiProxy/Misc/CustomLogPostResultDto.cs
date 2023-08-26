using System.Net;

namespace AzureMonitorDataCollectorApiProxy.Misc
{
    public class CustomLogPostResultDto
    {
        public HttpStatusCode HttpStatusCode { get; set; }

        public string ResponseMessage { get; set; }
    }
}