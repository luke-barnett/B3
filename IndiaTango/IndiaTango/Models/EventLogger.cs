using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace IndiaTango.Models
{
    public delegate void EventLoggedEventHandler(object sender, EventLoggedArgs e);

    public class EventLoggedArgs : EventArgs
    {
        public readonly string EventLog;
        public readonly string Filename;

        public EventLoggedArgs(string eventLog, string filename)
        {
            EventLog = eventLog;
            Filename = filename;
        }

    }

    public static class EventLogger
    {
        #region PrivateMembers
        private const string Info = "INFO";
        private const string Warning = "WARNING";
        private const string Error = "ERROR";
        private static StreamWriter _writer;
        private readonly static object Mutex = new object();
        #endregion

        public static event EventLoggedEventHandler Changed;

        #region Properties
        /// <summary>
        /// Returns the location of the log file on disc
        /// </summary>
        public static string LogFilePath
        {
            get { return Path.Combine(Common.AppDataPath, "Logs", "log.txt"); }
        }

        /// <summary>
        /// The path to sensor logs
        /// </summary>
        /// <param name="sensorName">The sensor</param>
        /// <returns>The path for the sensor logs</returns>
        public static string GetSensorLogPath(string sensorName)
        {
            return Path.Combine(Common.AppDataPath, "Logs", "SensorLogs", sensorName + ".txt");
        }

        /// <summary>
        /// The site log path
        /// </summary>
        /// <param name="dataSet">The dataset</param>
        /// <returns>The dataset's site log path</returns>
        public static string GetSiteLogPath(Dataset dataSet)
        {
            return Path.Combine(Common.AppDataPath, "Logs", dataSet.IdentifiableName, "log.txt");
        }

        /// <summary>
        /// The sensors site log path
        /// </summary>
        /// <param name="sensorName">The sensor</param>
        /// <param name="dataSet">The dataset</param>
        /// <returns>The path to the site sensor</returns>
        public static string GetSensorLogPathForSensorBelongingToSite(string sensorName, Dataset dataSet)
        {
            return Path.Combine(Common.AppDataPath, "Logs", dataSet.IdentifiableName, "SensorLogs", sensorName + ".txt");
        }

        /// <summary>
        /// Returns the format string used to display the date and time for each logged event
        /// </summary>
        public static string TimeFormatString
        {
            get { return "dd/MM/yyyy HH:mm:ss"; }
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

        /// <summary>
        /// Writes the log
        /// </summary>
        /// <param name="logType">The type of the log</param>
        /// <param name="threadName">The name of the thread making the log</param>
        /// <param name="eventDetails">The log details</param>
        /// <param name="destFile">The location of the file</param>
        /// <returns>The result</returns>
        private static string LogBase(string logType, string threadName, string eventDetails, string destFile)
        {
            if (String.IsNullOrWhiteSpace(threadName))
                threadName = "<No Thread Name>";

            if (String.IsNullOrWhiteSpace(eventDetails))
                throw new ArgumentNullException("Parameter 'eventDetails' cannot be null or empty");

            if (threadName.Contains("IndiaTango."))
                threadName = threadName.Substring("IndiaTango.".Length);

            string logString = DateTime.Now.ToString(TimeFormatString) + "    " + logType.PadRight(10).Substring(0, 10) + " " + threadName.PadRight(25).Substring(0, 25) + " " + eventDetails;

            WriteLogToFile(logString, destFile);

            OnLogEvent(null, new EventLoggedArgs(logString, destFile));

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
        /// <param name="site">The site this log belongs to</param>
        /// <param name="threadName">The name of the thread calling this Method. For Background workers, supply a brief description of the threads purpose</param>
        /// <param name="eventDetails">The specific details of the event that are to be written to file</param>
        /// <returns></returns>
        public static string LogInfo(Dataset site, string threadName, string eventDetails)
        {
            return site == null ? LogBase(Info, threadName, eventDetails, null) : LogBase(Info, threadName, eventDetails, GetSiteLogPath(site));
        }

        /// <summary>
        /// Logs a warning event to the log file, containing the current time, thread name and details of the event
        /// </summary>
        /// <param name="site">The site this log belongs to</param>
        /// <param name="threadName">The name of the thread calling this Method. For Background workers, supply a brief description of the threads purpose</param>
        /// <param name="eventDetails">The specific details of the event that are to be written to file</param>
        /// <returns></returns>
        public static string LogWarning(Dataset site, string threadName, string eventDetails)
        {
            return site == null ? LogBase(Warning, threadName, eventDetails, null) : LogBase(Warning, threadName, eventDetails, GetSiteLogPath(site));
        }

        /// <summary>
        /// Logs an error event to the log file, containing the current time, thread name and details of the event
        /// </summary>
        /// <param name="site">The site this log belongs to</param>
        /// <param name="threadName">The name of the thread calling this Method. For Background workers, supply a brief description of the threads purpose</param>
        /// <param name="eventDetails">The specific details of the event that are to be written to file</param>
        /// <returns></returns>
        public static string LogError(Dataset site, string threadName, string eventDetails)
        {
            return site == null ? LogBase(Error, threadName, eventDetails, null) : LogBase(Error, threadName, eventDetails, GetSiteLogPath(site));
        }

        /// <summary>
        /// Logs a change to a sensor, to an individual file for each sensor.
        /// </summary>
        /// <param name="site">The site this log belongs to</param>
        /// <param name="sensorName"></param>
        /// <param name="eventDetails"></param>
        /// <returns></returns>
        public static string LogSensorInfo(Dataset site, string sensorName, string eventDetails)
        {
            return site == null ? LogBase(Info, sensorName, eventDetails, GetSensorLogPath(sensorName)) : LogBase(Info, sensorName, eventDetails, GetSensorLogPathForSensorBelongingToSite(sensorName, site));
        }

        /// <summary>
        /// Retrives the lastest 20 logs
        /// </summary>
        /// <returns>The latest 20 logs</returns>
        public static string GetLast20()
        {
            var q = new Queue<String>();

            for (var i = 0; i < 20; i++)
                q.Enqueue("");

            //Super line to fix all the problems (Totally not an XP thing btw)
            if (!File.Exists(LogFilePath))
                return "";

            using (var sr = new StreamReader(LogFilePath))
            {
                while (sr.Peek() != -1)
                {
                    q.Dequeue();
                    q.Enqueue(sr.ReadLine() + "\n");
                }
            }

            return q.Aggregate("", (current, s) => current + s);
        }

        /// <summary>
        /// Gets the latest 20 logs from a specific log file
        /// </summary>
        /// <param name="file">The filename of the log</param>
        /// <returns>The latest 20 logs</returns>
        public static string[] GetLast20FromFile(string file)
        {
            var list = new List<string>();

            if (File.Exists(file))
                using (var sr = new StreamReader(file))
                {
                    while (!sr.EndOfStream)
                        list.Add(sr.ReadLine());
                }

            return list.SkipWhile((x, index) => index < list.Count - 20).ToArray();
        }

        /// <summary>
        /// Gets the set of log files available
        /// </summary>
        /// <returns>The log filenames</returns>
        public static string[] GetLogFiles()
        {
            var list = new List<string>();

            var basePath = Path.Combine(Common.AppDataPath, "Logs");

            list.Add(LogFilePath);

            var sites = Directory.GetDirectories(basePath);

            foreach (var site in sites)
            {
                list.AddRange(Directory.GetFiles(site));
                if (Directory.Exists(Path.Combine(site, "SensorLogs")))
                    list.AddRange(Directory.GetFiles(Path.Combine(site, "SensorLogs")));
            }

            return list.ToArray();
        }
        #endregion
    }
}
