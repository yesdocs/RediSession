using System;
using System.Web.Configuration;

namespace RediSessionLibrary
{
    /// <summary>
    /// Holds and reads values for configuring a Redis Connection
    /// </summary>
    public class RedisConfiguration
    {
        #region Config properties
        /// <summary>
        /// Name of the Redis Server. Defaults to 'localhost'.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// The configured Port number of Redis server. Defaults to 5757.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Timeout in minutes. Defaults to 30 mins.
        /// </summary>
        public int SessionTimeout { get; set; }
        #endregion

        #region ctors
        /// <summary>
        /// Creates a Configuration object from a SessionStateSection object
        /// </summary>
        /// <param name="config">the SessionStateSection object</param>
        public RedisConfiguration(SessionStateSection config)
            : base()
        {
            Port = 5757;
            Host = "localhost";
            SessionTimeout = 30;

            if (config != null)
                ReadConfiguration(config);
        }
        #endregion

        #region Helper Functions

        /// <summary>
        /// Reads the values from a SessionStateSection config object
        /// </summary>
        /// <param name="config">hte config object to read</param>
        private void ReadConfiguration(SessionStateSection config)
        {
            // Read Host and Port
            string connect = config.StateConnectionString;
            if (!string.IsNullOrEmpty(connect) && connect.StartsWith("tcp="))
            {   // eg tcp=localhost:6379
                string[] parts = connect.Substring(4).Split(':');
                if (parts != null && parts.Length > 1)
                {
                    Port = Int32.Parse(parts[1]);
                    Host = parts[0];
                }
            }

            // Read timeout value
            if (config.Timeout.Minutes > 0)
                SessionTimeout = config.Timeout.Minutes;
        }
        #endregion
    }
}
