using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IndiaTango.Models;
using NUnit.Framework;

namespace IndiaTango.Tests
{
    [TestFixture]
    public class EventLoggerTest
    {
        [Test]
        public void InformationLogTest()
        {
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " INFO     Thread-1             Application started", EventLogger.LogInfo("Thread-1", "Application started"));
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " INFO     Thread-1             Loaded CSV File", EventLogger.LogInfo("Thread-1", "Loaded CSV File"));
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " INFO     A_LONG_THREAD_NAME_I Loaded CSV File", EventLogger.LogInfo("A_LONG_THREAD_NAME_IS_THIS", "Loaded CSV File"));
        }

        [Test]
        public void WarngingLogTest()
        {
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " WARNING  GUI Thread           Levels of Low IQ detected", EventLogger.LogWarning("GUI Thread", "Levels of Low IQ detected"));
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " WARNING  GUI Thread           Just another silly test", EventLogger.LogWarning("GUI Thread", "Just another silly test"));
        }

        [Test]
        public void ErrorLogTest()
        {
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " ERROR    Thread-0             Application fatal error or something", EventLogger.LogError("Thread-0", "Application fatal error or something"));
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " ERROR    Thread-0             User has uploaded a picture of a cat, not a .csv file", EventLogger.LogError("Thread-0", "User has uploaded a picture of a cat, not a .csv file"));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InformationLogNullThreadTest()
        {
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " INFO                         Levels of Low IQ detected", EventLogger.LogInfo("", "Levels of Low IQ detected"));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InformationLogNullDetailsTest()
        {
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " INFO     GUI Thread           ", EventLogger.LogInfo("GUI Thread", ""));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void WarningLogNullThreadTest()
        {
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " WARNING                      Levels of Low IQ detected", EventLogger.LogWarning("", "Levels of Low IQ detected"));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void WarningLogNullDetailsTest()
        {
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " WARNING  GUI Thread           ", EventLogger.LogWarning("GUI Thread", ""));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ErrorLogNullThreadTest()
        {
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " ERROR                        Levels of Low IQ detected", EventLogger.LogError("", "Levels of Low IQ detected"));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ErrorLogNullDetailsTest()
        {
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " ERROR    GUI Thread           ", EventLogger.LogError("GUI Thread", ""));
        }
    }
}
