using AzureMonitorDataCollectorApiProxy.Misc;

namespace AzureMonitorDataCollectorApiProxy.Services
{
    public interface IDataCollectorApi
    {
        public Task<CustomLogPostResultDto> PostCustomLogAsync(string jsonMessage, string logType, string timeStamp = "");
    }
}