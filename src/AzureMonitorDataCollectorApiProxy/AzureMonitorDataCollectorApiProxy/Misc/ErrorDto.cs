using System.Net;

namespace AzureMonitorDataCollectorApiProxy.Misc
{
    /// <summary>
    /// 
    /// </summary>
    public class ErrorDto
    {
        /// <summary>
        /// Status code to return to the user.
        /// </summary>
        public HttpStatusCode HttpStatusCode { get; set; }

        /// <summary>
        /// Response message to return to the user.
        /// </summary>
        public string ResponseMessage { get; set; } = null!;
    }
}