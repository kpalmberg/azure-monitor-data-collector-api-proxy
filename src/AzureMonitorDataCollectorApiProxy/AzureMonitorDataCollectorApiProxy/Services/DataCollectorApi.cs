using System.Security.Cryptography;
using System.Text;
using System.Net.Http.Headers;

namespace AzureMonitorDataCollectorApiProxy.Services
{
    public class DataCollectorApi : IDataCollectorApi
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string apiVersion = "2016-04-01";

        public DataCollectorApi(IHttpClientFactory httpClientFactory, IConfiguration configuration) {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="logAnalyticsWorkspaceKey"></param>
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

        public async Task PostCustomLogAsync(string jsonMessage, string logType, string timeStamp = "")
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

                if (!response.IsSuccessStatusCode)
                {
                    HttpContent responseContent = response.Content;
                    string result = responseContent.ReadAsStringAsync().Result;
                    Console.WriteLine("Return Result: " + result);
                }
                else
                {

                }
            }
            catch (Exception excep)
            {
                Console.WriteLine("API Post Exception: " + excep.Message);
            }
        }
    }
}