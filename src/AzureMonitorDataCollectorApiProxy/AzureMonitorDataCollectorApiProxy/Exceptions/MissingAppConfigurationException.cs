namespace AzureMonitorDataCollectorApiProxy.Exceptions
{
    /// <summary>
    /// Custom exception for when required app configuration is missing.
    /// </summary>
    public class MissingAppConfigurationException : Exception
    {
        /// <summary>
        /// Exception with default values.
        /// </summary>
        public MissingAppConfigurationException()
        {
        }

        /// <summary>
        /// Exception with a string message.
        /// </summary>
        /// <param name="message">String message for the exception.</param>
        public MissingAppConfigurationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Exception with a string message and inner exception.
        /// </summary>
        /// <param name="message">String message for the exception.</param>
        /// <param name="inner">Inner exception.</param>
        public MissingAppConfigurationException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}