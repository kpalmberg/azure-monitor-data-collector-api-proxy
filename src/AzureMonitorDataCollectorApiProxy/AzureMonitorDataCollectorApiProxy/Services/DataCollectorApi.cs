﻿using System.Security.Cryptography;
using System.Text;
using System.Net.Http.Headers;
using System.Net;
using AzureMonitorDataCollectorApiProxy.Misc;
using AzureMonitorDataCollectorApiProxy.Exceptions;
using AzureMonitorDataCollectorApiProxy.Constants;
using AzureMonitorDataCollectorApiProxy.Extensions;

namespace AzureMonitorDataCollectorApiProxy.Services
{
    /// <inheritdoc />
    public class DataCollectorApi : IDataCollectorApi
    {
        private readonly ILogger<DataCollectorApi> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string apiVersion = "2016-04-01";

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectorApi"/> class.
        /// </summary>
        public DataCollectorApi(ILogger<DataCollectorApi> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        /// <inheritdoc />
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
                #pragma warning disable CA2000 // HTTP client factory manages client lifetime
                HttpClient client = _httpClientFactory.CreateClient();
                #pragma warning restore CA2000
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("Log-Type", logType);
                client.DefaultRequestHeaders.Add("Authorization", signature);
                client.DefaultRequestHeaders.Add("x-ms-date", datestring);

                if (!string.IsNullOrEmpty(timeStamp))
                {
                    // Add in optional time generated field if it's specified
                    client.DefaultRequestHeaders.Add("time-generated-field", timeStamp);
                }

                // If charset=utf-8 is part of the content-type header, the API call may return forbidden.
                using HttpContent httpContent = new StringContent(jsonMessage, Encoding.UTF8);
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                HttpResponseMessage response = await client.PostAsync(new Uri(url), httpContent).ConfigureAwait(false);

                return GetCustomLogOperationResult((int)response.StatusCode, response.Content.ReadAsStringAsync().Result);
            }
            catch (MissingAppConfigurationException ex)
            {
                _logger.QuickLogErrorWithException(InternalErrorMessages.MissingRequiredAppSetting, ex);

                return new CustomLogPostResultDto
                {
                    HttpStatusCode = StatusCodes.Status500InternalServerError,
                    ResponseMessage = "Failed to make call to Log Analytics REST API."
                };
            }
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

        /// <summary>
        /// Reads in app configuration such as environmental variables.
        /// </summary>
        /// <param name="configurationSettingName">Name of the app configuration to read.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private string GetConfigurationString(string configurationSettingName)
        {
            string? value = _configuration.GetValue<string>(configurationSettingName) ?? throw new MissingAppConfigurationException(InternalErrorMessages.MissingRequiredAppSetting);
            return value;
        }

        /// <summary>
        /// Processes status codes from the Data Collector API call. See the following doc for the full list: https://learn.microsoft.com/en-us/azure/azure-monitor/logs/data-collector-api?tabs=c-sharp#return-codes
        /// </summary>
        /// <param name="httpStatusCode">HTTP status code from the completed Data Collector API call.</param>
        /// <param name="httpContentResult">Response messages from the completed Data Collector API call.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static CustomLogPostResultDto GetCustomLogOperationResult(int httpStatusCode, string httpContentResult)
        {
            switch (httpStatusCode)
            {
                case StatusCodes.Status200OK:
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = StatusCodes.Status200OK,
                        ResponseMessage = "Request received for processing. Operation finished successfully."
                    };
                case StatusCodes.Status400BadRequest when httpContentResult.Contains("InactiveCustomer", StringComparison.OrdinalIgnoreCase):
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = StatusCodes.Status400BadRequest,
                        ResponseMessage = "The workspace has been closed."
                    };
                case StatusCodes.Status400BadRequest when httpContentResult.Contains("InvalidApiVersion", StringComparison.OrdinalIgnoreCase):
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = StatusCodes.Status400BadRequest,
                        ResponseMessage = "The API version that you specified wasn't recognized by the service."
                    };
                case StatusCodes.Status400BadRequest when httpContentResult.Contains("InvalidCustomerId", StringComparison.OrdinalIgnoreCase):
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = StatusCodes.Status400BadRequest,
                        ResponseMessage = "The specified workspace ID is invalid."
                    };
                case StatusCodes.Status400BadRequest when httpContentResult.Contains("InvalidDataFormat", StringComparison.OrdinalIgnoreCase):
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = StatusCodes.Status400BadRequest,
                        ResponseMessage = "An invalid JSON was submitted. The response body might contain more information about how to resolve the error."
                    };
                case StatusCodes.Status400BadRequest when httpContentResult.Contains("InvalidLogType", StringComparison.OrdinalIgnoreCase):
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = StatusCodes.Status400BadRequest,
                        ResponseMessage = "The specified log type contained special characters or numerics."
                    };
                case StatusCodes.Status400BadRequest when httpContentResult.Contains("MissingApiVersion", StringComparison.OrdinalIgnoreCase):
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = StatusCodes.Status400BadRequest,
                        ResponseMessage = "The API version wasn’t specified."
                    };
                case StatusCodes.Status400BadRequest when httpContentResult.Contains("MissingContentType", StringComparison.OrdinalIgnoreCase):
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = StatusCodes.Status400BadRequest,
                        ResponseMessage = "The content type wasn’t specified."
                    };
                case StatusCodes.Status400BadRequest when httpContentResult.Contains("MissingLogType", StringComparison.OrdinalIgnoreCase):
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = StatusCodes.Status400BadRequest,
                        ResponseMessage = "The required value log type wasn’t specified."
                    };
                case StatusCodes.Status400BadRequest when httpContentResult.Contains("UnsupportedContentType", StringComparison.OrdinalIgnoreCase):
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = StatusCodes.Status400BadRequest,
                        ResponseMessage = "The content type wasn't set to application/json."
                    };
                case StatusCodes.Status403Forbidden when httpContentResult.Contains("InvalidAuthorization", StringComparison.OrdinalIgnoreCase):
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = StatusCodes.Status403Forbidden,
                        ResponseMessage = "The service failed to authenticate the request. Verify that the workspace ID and connection key are valid."
                    };
                case StatusCodes.Status404NotFound:
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = StatusCodes.Status404NotFound,
                        ResponseMessage = "Either the provided URL is incorrect or the request is too large."
                    };
                case StatusCodes.Status429TooManyRequests:
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = StatusCodes.Status429TooManyRequests,
                        ResponseMessage = "The service is experiencing a high volume of data from your account. Please retry the request later."
                    };
                case StatusCodes.Status500InternalServerError when httpContentResult.Contains("UnspecifiedError", StringComparison.OrdinalIgnoreCase):
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = StatusCodes.Status500InternalServerError,
                        ResponseMessage = "The service encountered an internal error. Please retry the request."
                    };
                case StatusCodes.Status503ServiceUnavailable when httpContentResult.Contains("ServiceUnavailable", StringComparison.OrdinalIgnoreCase):
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = StatusCodes.Status503ServiceUnavailable,
                        ResponseMessage = "The service currently is unavailable to receive requests. Please retry your request."
                    };
                default:
                    return new CustomLogPostResultDto
                    {
                        HttpStatusCode = httpStatusCode,
                        ResponseMessage = $"Status code {httpStatusCode} received, no specific response message available."
                    };
            }
        }
    }
}