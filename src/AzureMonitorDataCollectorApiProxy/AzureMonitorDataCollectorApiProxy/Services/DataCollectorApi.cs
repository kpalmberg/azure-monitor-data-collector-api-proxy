using System.Security.Cryptography;
using System.Text;
using System.Net.Http.Headers;
using System.Net;
using AzureMonitorDataCollectorApiProxy.Misc;

namespace AzureMonitorDataCollectorApiProxy.Services
{
    public class DataCollectorApi : IDataCollectorApi
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string apiVersion = "2016-04-01";

        public DataCollectorApi(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        /// <summary>
        /// Constructs the HMAC-SHA256 header value for the Log Analytics authorization signature.
        /// </summary>
        /// <param name="message">Message to hash.</param>
        /// <param name="logAnalyticsWorkspaceKey">Log Analytics workspace primary OR secondary key.</param>
        /// <returns></returns>
        private static string BuildApiSignature(string message, string logAnalyticsWorkspaceKey)
        {
            ASCIIEncoding encoding = new();
            byte[] keyByte = Convert.FromBase64String(logAnalyticsWorkspaceKey);
            byte[] messageBytes = encoding.GetBytes(message);
            using var hmacsha256 = new HMACSHA256(keyByte);
            byte[] hash = hmacsha256.ComputeHash(messageBytes);
            return Convert.ToBase64String(hash);
        }

        private string GetConfigurationString(string configurationSettingName)
        {
            string? value = _configuration.GetValue<string>(configurationSettingName);

            if (value == null)
            {
                // Required environmental variable could not be found
                throw new Exception(); // TODO
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// Processes status codes from the Data Collector API call. See the following doc for the full list: https://learn.microsoft.com/en-us/azure/azure-monitor/logs/data-collector-api?tabs=c-sharp#return-codes
        /// </summary>
        /// <param name="httpStatusCode">HTTP status code from the completed Data Collector API call.</param>
        /// <param name="httpContentResult">Response messages from the completed Data Collector API call.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private CustomLogPostResultDto GetCustomLogOperationResult(HttpStatusCode httpStatusCode, string httpContentResult)
        {
            switch (httpStatusCode)
            {
                case HttpStatusCode.OK when httpContentResult.Contains("TEST", StringComparison.OrdinalIgnoreCase):
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = HttpStatusCode.BadRequest,
                        ResponseMessage = "Request received for processing. Operation finished successfully."
                    };
                case HttpStatusCode.BadRequest when httpContentResult.Contains("InactiveCustomer", StringComparison.OrdinalIgnoreCase):
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = HttpStatusCode.BadRequest,
                        ResponseMessage = "The workspace has been closed."
                    };
                case HttpStatusCode.BadRequest when httpContentResult.Contains("InvalidApiVersion", StringComparison.OrdinalIgnoreCase):
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = HttpStatusCode.BadRequest,
                        ResponseMessage = "The API version that you specified wasn't recognized by the service."
                    };
                case HttpStatusCode.BadRequest when httpContentResult.Contains("InvalidCustomerId", StringComparison.OrdinalIgnoreCase):
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = HttpStatusCode.BadRequest,
                        ResponseMessage = "The specified workspace ID is invalid."
                    };
                case HttpStatusCode.BadRequest when httpContentResult.Contains("InvalidDataFormat", StringComparison.OrdinalIgnoreCase):
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = HttpStatusCode.BadRequest,
                        ResponseMessage = "An invalid JSON was submitted. The response body might contain more information about how to resolve the error."
                    };
                case HttpStatusCode.BadRequest when httpContentResult.Contains("InvalidLogType", StringComparison.OrdinalIgnoreCase):
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = HttpStatusCode.BadRequest,
                        ResponseMessage = "The specified log type contained special characters or numerics."
                    };
                case HttpStatusCode.BadRequest when httpContentResult.Contains("MissingApiVersion", StringComparison.OrdinalIgnoreCase):
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = HttpStatusCode.BadRequest,
                        ResponseMessage = "The API version wasn’t specified."
                    };
                case HttpStatusCode.BadRequest when httpContentResult.Contains("MissingContentType", StringComparison.OrdinalIgnoreCase):
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = HttpStatusCode.BadRequest,
                        ResponseMessage = "The content type wasn’t specified."
                    };
                case HttpStatusCode.BadRequest when httpContentResult.Contains("MissingLogType", StringComparison.OrdinalIgnoreCase):
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = HttpStatusCode.BadRequest,
                        ResponseMessage = "The required value log type wasn’t specified."
                    };
                case HttpStatusCode.BadRequest when httpContentResult.Contains("UnsupportedContentType", StringComparison.OrdinalIgnoreCase):
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = HttpStatusCode.BadRequest,
                        ResponseMessage = "The content type wasn't set to application/json."
                    };
                case HttpStatusCode.Forbidden when httpContentResult.Contains("InvalidAuthorization", StringComparison.OrdinalIgnoreCase):
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = HttpStatusCode.Forbidden,
                        ResponseMessage = "The service failed to authenticate the request. Verify that the workspace ID and connection key are valid."
                    };
                case HttpStatusCode.NotFound:
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = HttpStatusCode.NotFound,
                        ResponseMessage = "Either the provided URL is incorrect or the request is too large."
                    };
                case HttpStatusCode.TooManyRequests:
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = HttpStatusCode.TooManyRequests,
                        ResponseMessage = "The service is experiencing a high volume of data from your account. Please retry the request later."
                    };
                case HttpStatusCode.InternalServerError when httpContentResult.Contains("UnspecifiedError", StringComparison.OrdinalIgnoreCase):
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = HttpStatusCode.InternalServerError,
                        ResponseMessage = "The service encountered an internal error. Please retry the request."
                    };
                case HttpStatusCode.ServiceUnavailable when httpContentResult.Contains("ServiceUnavailable", StringComparison.OrdinalIgnoreCase):
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = HttpStatusCode.ServiceUnavailable,
                        ResponseMessage = "The service currently is unavailable to receive requests. Please retry your request."
                    };
                default:
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = HttpStatusCode.Accepted,
                        ResponseMessage = $"Status code {HttpStatusCode.Accepted} received, no specific response message available."
                    };
            }
        }

        public async Task<CustomLogPostResultDto> PostCustomLogAsync(string jsonMessage, string logType, string timeStamp = "")
        {
            try
            {
                // Setup requires vars
                string logAnalyticsWorkspaceId = GetConfigurationString("LOG__ANALYTICS__WORKSPACE__ID");
                string logAnalyticsWorkspaceKey = GetConfigurationString("LOG__ANALYTICS__WORKSPACE__KEY");
                string url = "https://" + logAnalyticsWorkspaceId + ".ods.opinsights.azure.com/api/logs?api-version=" + apiVersion;

                // Build auth signature
                string datestring = DateTime.UtcNow.ToString("r");
                var jsonBytes = Encoding.UTF8.GetBytes(jsonMessage);
                string stringToHash = "POST\n" + jsonBytes.Length + "\napplication/json\n" + "x-ms-date:" + datestring + "\n/api/logs";
                string hashedString = BuildApiSignature(stringToHash, logAnalyticsWorkspaceKey);
                string signature = "SharedKey " + logAnalyticsWorkspaceId + ":" + hashedString;

                // Configure HTTP client
                HttpClient client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                //client.DefaultRequestHeaders.Add("Log-Type", logType);
                client.DefaultRequestHeaders.Add("Authorization", signature);
                client.DefaultRequestHeaders.Add("x-ms-date", datestring);

                if (!string.IsNullOrEmpty(timeStamp))
                {
                    // Add in optional time generated field if it's specified
                    client.DefaultRequestHeaders.Add("time-generated-field", timeStamp);
                }

                // If charset=utf-8 is part of the content-type header, the API call may return forbidden.
                HttpContent httpContent = new StringContent(jsonMessage, Encoding.UTF8);
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                HttpResponseMessage response = await client.PostAsync(new Uri(url), httpContent);

                return GetCustomLogOperationResult(response.StatusCode, response.Content.ReadAsStringAsync().Result);
            }
            catch (Exception excep)
            {
                Console.WriteLine("API Post Exception: " + excep.Message);

                return new CustomLogPostResultDto
                {
                    HttpStatusCode = HttpStatusCode.InternalServerError,
                    ResponseMessage = "Failed to make call to Log Analytics REST API."
                };
            }
        }
    }
}