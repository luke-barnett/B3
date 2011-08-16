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
        private const string Info = "INFO";
        private const string Warning = "WARNING";
        private const string Error = "ERROR";
        private static StreamWriter writer = null;

        public static string LogFilePath
        {
            get { return Path.Combine(Common.AppDataPath,"Logs","log.txt") ; }
        }

        private static void WriteLogToFile(string log)
        {
            if(!Directory.Exists(LogFilePath))
                Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath));

            if (writer == null)
                writer = File.AppendText(LogFilePath);
            
            writer.WriteLine(log);
        }

        private static string LogBase(string logType, Thread thread, string eventDetails)
        {
            if (thread == null)
                throw new ArgumentNullException("Parameter 'thread' cannot be null or empty");

            if (String.IsNullOrWhiteSpace(thread.Name))
                throw new ArgumentNullException("Thread name cannot be null or empty");

            if (String.IsNullOrWhiteSpace(eventDetails))
                throw new ArgumentNullException("Parameter 'eventDetails' cannot be null or empty");
            
            string logString = DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " " + logType.PadRight(8).Substring(0, 8) + " " + thread.Name.PadRight(20).Substring(0, 20) + " " + eventDetails;
            
            WriteLogToFile(logString);

            return logString;
        }

        public static string LogInfo(Thread thread, string eventDetails)
        {
            return LogBase(Info, thread, eventDetails);
        }

        public static string LogWarning(Thread thread, string eventDetails)
        {
            return LogBase(Warning, thread, eventDetails);
        }

        public static string LogError(Thread thread, string eventDetails)
        {
            return LogBase(Error, thread, eventDetails);
        }
    }
}
