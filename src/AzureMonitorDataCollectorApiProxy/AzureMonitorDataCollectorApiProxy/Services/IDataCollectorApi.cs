using AzureMonitorDataCollectorApiProxy.Misc;

namespace AzureMonitorDataCollectorApiProxy.Services
{
    /// <summary>
    /// Interface for interacting with the Azure Data Collector API.
    /// </summary>
    public interface IDataCollectorApi
    {
        /// <summary>
        /// Creates custom log(s) in a Log Analytics workspace.
        /// </summary>
        /// <param name="jsonMessage">JSON payload as a string.</param>
        /// <param name="logType">The type of the Log Analytics log.</param>
        /// <param name="timeStamp">Optional timestamp property containing the timestamp of the data item(s).</param>
        /// <returns>DTO of the processing result.</returns>
        public Task<CustomLogPostResultDto> PostCustomLogAsync(string jsonMessage, string logType, string timeStamp = "");
    }
}