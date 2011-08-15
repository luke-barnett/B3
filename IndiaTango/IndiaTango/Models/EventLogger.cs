using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IndiaTango.Models
{
    public static class EventLogger
    {
        private const string Info = "INFO";
        private const string Warning = "WARNING";
        private const string Error = "ERROR";

        private static string _logFilePath = Path.Combine(new string[]{
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "IndiaTango",
            "Logs",
            "Log.txt"
        });

        public static string LogFilePath
        {
            get { return _logFilePath; }
            set { _logFilePath = value; }
        }

        private static void WriteLogToFile(string log)
        {
            //TODO Check directory exists and create it

            using (StreamWriter writer = File.AppendText(LogFilePath))
            {
                writer.WriteLine(log);
            }
        }

        private static string LogBase(string logType, string thread, string eventDetails)
        {
            if (String.IsNullOrWhiteSpace(thread))
                throw new ArgumentNullException("Parameter 'thread' cannot be null or empty");

            if (String.IsNullOrWhiteSpace(eventDetails))
                throw new ArgumentNullException("Parameter 'eventDetails' cannot be null or empty");
            
            string logString = DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " " + logType.PadRight(8).Substring(0, 8) + " " + thread.PadRight(20).Substring(0, 20) + " " + eventDetails;
            
            WriteLogToFile(logString);

            return logString;
        }

        

        public static string LogInfo(string thread, string eventDetails)
        {
            return LogBase(Info, thread, eventDetails);
        }

        public static string LogWarning(string thread, string eventDetails)
        {
            return LogBase(Warning, thread, eventDetails);
        }

        public static string LogError(string thread, string eventDetails)
        {
            return LogBase(Error, thread, eventDetails);
        }
    }
}
