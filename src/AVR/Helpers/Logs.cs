using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AVR
{
    /// <summary>
    /// Exception service to handle and store exception
    /// </summary>
    public static class Logs
    {
        #region Private Data Member

        /// <summary>
        /// main logger object to access log4net member and functions
        /// </summary>
        private static log4net.ILog _logger { get; set; }

        #endregion

        #region Public Data Members

        #endregion

        #region Static Data Member

        #endregion

        #region Constructor
        /// <summary>
        /// Static constructor
        /// </summary>
        static Logs()
        {
            _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        }
        #endregion

        #region Private Member Functions

        #endregion

        #region Public Member Function

        #endregion

        #region Static Member Function

        /// <summary>
        /// Log your debug messages
        /// </summary>
        /// <param name="message">Object: Message to log</param>
        public static void LogDebug(object message)
        {
            _logger.Debug(message);
        }

        /// <summary>
        /// Log your debug messages and exception
        /// </summary>
        /// <param name="message">Object: Message to log</param>
        /// <param name="exception">Exception: Exception to log</param>
        public static void LogDebug(object message, Exception exception)
        {
            _logger.Debug(message, exception);
        }

        /// <summary>
        /// Log your error messages
        /// </summary>
        /// <param name="message">Object: Message to log</param>
        public static void LogError(object message)
        {
            _logger.Error(message);
        }

        /// <summary>
        /// Log your error messages and exception
        /// </summary>
        /// <param name="message">Object: Message to log</param>
        /// <param name="exception">Exception: Exception to log</param>
        public static void LogError(object message, Exception exception)
        {
            _logger.Error(message, exception);
        }
        #endregion
    }
}