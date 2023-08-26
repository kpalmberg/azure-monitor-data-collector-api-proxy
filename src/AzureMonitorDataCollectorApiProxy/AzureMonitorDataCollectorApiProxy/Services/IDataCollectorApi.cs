namespace AzureMonitorDataCollectorApiProxy.Services
{
    public interface IDataCollectorApi
    {
        public Task PostCustomLogAsync(string jsonMessage, string logType, string timeStamp = "");
    }
}