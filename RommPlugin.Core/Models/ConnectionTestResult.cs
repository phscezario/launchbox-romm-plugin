namespace RommPlugin.Core.Services
{
    /// <summary>
    /// Represents the result of a connection test to the RomM server.
    /// </summary>
    public class ConnectionTestResult
    {
        /// <summary>
        /// Gets or sets whether the connection test was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets a message describing the test result.
        /// </summary>
        public string Message { get; set; }
    }
}
