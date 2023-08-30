namespace AzureMonitorDataCollectorApiProxy.Extensions
{
    /// <summary>
    /// High-performance logging methods.
    /// </summary>
    public static partial class LoggerMessaging
    {
        /// <summary>
        /// Writes an information log message.
        /// </summary>
        [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "{Message}")]
        public static partial void QuickLogInformation(this ILogger logger, string message);

        /// <summary>
        /// Writes a debug log message.
        /// </summary>
        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "{Message}")]
        public static partial void QuickLogDebug(this ILogger logger, string message);


        /// <summary>
        /// Writes a warning log message.
        /// </summary>
        [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "{Message}")]
        public static partial void QuickLogWarning(this ILogger logger, string message);


        /// <summary>
        /// Writes an error log message.
        /// </summary>

        [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "{Message}")]
        public static partial void QuickLogError(this ILogger logger, string message);

        /// <summary>
        /// Writes an error log message with exception.
        /// </summary>

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "{Message}")]
        public static partial void QuickLogErrorWithException(this ILogger logger, string message, Exception ex);


        /// <summary>
        /// Writes a critical log message with exception.
        /// </summary>
        [LoggerMessage(EventId = 5, Level = LogLevel.Critical, Message = "{Message}")]
        public static partial void QuickLogCritical(this ILogger logger, string message, Exception ex);
    }
}