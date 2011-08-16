using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace IndiaTango.Models
{
    public static class EventLogger
    {
        #region PrivateMembers
        private const string Info = "INFO";
        private const string Warning = "WARNING";
        private const string Error = "ERROR";
        private const string _timeFormatString = "dd/MM/yyyy HH:MM:ss.fff";
        private static StreamWriter _writer;
        private readonly static object Mutex = new object();
        #endregion

        #region Properties
        /// <summary>
        /// Returns the location of the log file on disc
        /// </summary>
        public static string LogFilePath
        {
            get { return Path.Combine(Common.AppDataPath,"Logs","log.txt") ; }
        }

        /// <summary>
        /// Returns the format string used to display the date and time for each logged event
        /// </summary>
        public static string TimeFormatString
        {
            get { return _timeFormatString; }
        }
        #endregion

        #region PrivateMethods
        private static void WriteLogToFile(string log)
        {
            if(!Directory.Exists(LogFilePath))
                Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath));

            lock (Mutex)
            {
                _writer = File.AppendText(LogFilePath);
                _writer.WriteLine(log);
                _writer.Close();
            }
        }

        private static string LogBase(string logType, string threadName, string eventDetails)
        {
            if (String.IsNullOrWhiteSpace(threadName))
                throw new ArgumentNullException("Thread name cannot be null or empty");

            if (String.IsNullOrWhiteSpace(eventDetails))
                throw new ArgumentNullException("Parameter 'eventDetails' cannot be null or empty");

            string logString = DateTime.Now.ToString(TimeFormatString) + " " + logType.PadRight(8).Substring(0, 8) + " " + threadName.PadRight(20).Substring(0, 20) + " " + eventDetails;
            
            WriteLogToFile(logString);

            return logString;
        }
        #endregion

        #region PublicMethods
        /// <summary>
        /// Logs an informative event to the log file, containing the current time, thread name and details of the event
        /// </summary>
        /// <param name="threadName">The name of the thread calling this Method. For Background workers, supply a brief description of the threads purpose</param>
        /// <param name="eventDetails">The specific details of the event that are to be written to file</param>
        /// <returns></returns>
        public static string LogInfo(string threadName, string eventDetails)
        {
            return LogBase(Info, threadName, eventDetails);
        }

        /// <summary>
        /// Logs a warning event to the log file, containing the current time, thread name and details of the event
        /// </summary>
        /// <param name="threadName">The name of the thread calling this Method. For Background workers, supply a brief description of the threads purpose</param>
        /// <param name="eventDetails">The specific details of the event that are to be written to file</param>
        /// <returns></returns>
        public static string LogWarning(string threadName, string eventDetails)
        {
            return LogBase(Warning, threadName, eventDetails);
        }

        /// <summary>
        /// Logs an error event to the log file, containing the current time, thread name and details of the event
        /// </summary>
        /// <param name="threadName">The name of the thread calling this Method. For Background workers, supply a brief description of the threads purpose</param>
        /// <param name="eventDetails">The specific details of the event that are to be written to file</param>
        /// <returns></returns>
        public static string LogError(string threadName, string eventDetails)
        {
            return LogBase(Error, threadName, eventDetails);
        }
        #endregion
    }
}
