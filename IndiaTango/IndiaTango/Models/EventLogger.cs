using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace IndiaTango.Models
{
    public delegate void EventLoggedEventHandler(object sender, EventLoggedArgs e);

    public class EventLoggedArgs : EventArgs
    {
        public readonly string EventLog;

        public EventLoggedArgs(string eventLog)
        {
            EventLog = eventLog;
        }

    }

    public static class EventLogger
    {
        #region PrivateMembers
        private const string Info = "INFO";
        private const string Warning = "WARNING";
        private const string Error = "ERROR";
        private const string _timeFormatString = "dd/MM/yyyy HH:mm:ss";
        private static StreamWriter _writer;
        private readonly static object Mutex = new object();
        private static int _logRefNum = 0;
        #endregion

        public static event EventLoggedEventHandler Changed;

        #region Properties
        /// <summary>
        /// Returns the location of the log file on disc
        /// </summary>
        public static string LogFilePath
        {
            get { return Path.Combine(Common.AppDataPath,"Logs","log.txt") ; }
        }

        public static string GetSensorLogPath(string sensorName)
        {
            return Path.Combine(Common.AppDataPath, "Logs", "SensorLog-" + sensorName + ".txt");
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
        /// <summary>
        /// Write a logged event to a log file.
        /// </summary>
        /// <param name="log">A string describing the event to log.</param>
        /// <param name="filePath">The path of the log file to write to. If null, uses the default log file.</param>
        private static void WriteLogToFile(string log, string filePath)
        {
            if (filePath == null)
                filePath = LogFilePath;

            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            lock (Mutex)
            {
                _writer = File.AppendText(filePath);
                _writer.WriteLine(log);
                _writer.Close();
                Debug.WriteLine(log);
            }
        }

        private static string LogBase(string logType, string threadName, string eventDetails, string destFile)
        {
            if (String.IsNullOrWhiteSpace(threadName))
                threadName = "<No Thread Name>";

            if (String.IsNullOrWhiteSpace(eventDetails))
                throw new ArgumentNullException("Parameter 'eventDetails' cannot be null or empty");

            if (threadName.Contains("IndiaTango."))
                threadName = threadName.Substring("IndiaTango.".Length);

            string logString = _logRefNum +" "+ DateTime.Now.ToString(TimeFormatString) + "    " + logType.PadRight(10).Substring(0, 10) + " " + threadName.PadRight(25).Substring(0, 25) + " " + eventDetails;
            
            WriteLogToFile(logString, destFile);

            OnLogEvent(null,new EventLoggedArgs(logString));

            _logRefNum++;

            return logString;
        }

        static void OnLogEvent(object o, EventLoggedArgs e)
        {
            if (Changed != null)
                Changed(o, e);
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
            return LogBase(Info, threadName, eventDetails, null);
        }

        /// <summary>
        /// Logs a warning event to the log file, containing the current time, thread name and details of the event
        /// </summary>
        /// <param name="threadName">The name of the thread calling this Method. For Background workers, supply a brief description of the threads purpose</param>
        /// <param name="eventDetails">The specific details of the event that are to be written to file</param>
        /// <returns></returns>
        public static string LogWarning(string threadName, string eventDetails)
        {
            return LogBase(Warning, threadName, eventDetails, null);
        }

        /// <summary>
        /// Logs an error event to the log file, containing the current time, thread name and details of the event
        /// </summary>
        /// <param name="threadName">The name of the thread calling this Method. For Background workers, supply a brief description of the threads purpose</param>
        /// <param name="eventDetails">The specific details of the event that are to be written to file</param>
        /// <returns></returns>
        public static string LogError(string threadName, string eventDetails)
        {
            return LogBase(Error, threadName, eventDetails, null);
        }

        /// <summary>
        /// Logs a change to a sensor, to an individual file for each sensor.
        /// </summary>
        /// <param name="sensorName"></param>
        /// <param name="eventDetails"></param>
        /// <returns></returns>
        public static string LogSensorInfo(string sensorName, string eventDetails)
        {
            return LogBase(Info, sensorName, eventDetails, GetSensorLogPath(sensorName));
        }

        public static string GetLast20()
        {
            var q = new Queue<String>();
            for (var i = 0; i < 20; i++)
                q.Enqueue("");
            using (var sr = new StreamReader(EventLogger.LogFilePath))
            {
                while (sr.Peek() != -1)
                {
                    q.Dequeue();
                    q.Enqueue(sr.ReadLine() + "\n");
                }
            }
            _logRefNum = Int32.Parse(q.Last().Split(' ')[0])+1;
            return q.Aggregate("", (current, s) => current + s);
        }
        #endregion
    }
}
