using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using IndiaTango.Models;
using NUnit.Framework;

namespace IndiaTango.Tests
{
    [TestFixture]
    public class EventLoggerTest
    {
        private Thread _threadOne;
        private Thread _longThreadName;
        private Thread _guiThread;
        private Thread _threadZero;
        private Thread[] _threadArray = new Thread[4];

        [SetUp]
        public void SetUp()
        {
            _threadOne = new Thread(EventLoggerTest.DummyDoWork);
            _threadOne.Name = "Thread-1";

            _longThreadName = new Thread(EventLoggerTest.DummyDoWork);
            _longThreadName.Name = "A_LONG_THREAD_NAME_IS_THIS";

            _guiThread = new Thread(EventLoggerTest.DummyDoWork);
            _guiThread.Name = "GUI Thread";

            _threadZero = new Thread(EventLoggerTest.DummyDoWork);
            _threadZero.Name = "Thread-0";
        }

        [Test]
        public void InformationLogTest()
        {
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " INFO     Thread-1             Application started", EventLogger.LogInfo(_threadOne.Name, "Application started"));
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " INFO     Thread-1             Loaded CSV File", EventLogger.LogInfo(_threadOne.Name, "Loaded CSV File"));
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " INFO     A_LONG_THREAD_NAME_I Loaded CSV File", EventLogger.LogInfo(_longThreadName.Name, "Loaded CSV File"));
        }

        [Test]
        public void WarningLogTest()
        {
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " WARNING  GUI Thread           Levels of Low IQ detected", EventLogger.LogWarning(_guiThread.Name, "Levels of Low IQ detected"));
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " WARNING  GUI Thread           Just another silly test", EventLogger.LogWarning(_guiThread.Name, "Just another silly test"));
        }

        [Test]
        public void ErrorLogTest()
        {
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " ERROR    Thread-0             Application fatal error or something", EventLogger.LogError(_threadZero.Name, "Application fatal error or something"));
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " ERROR    Thread-0             User has uploaded a picture of a cat, not a .csv file", EventLogger.LogError(_threadZero.Name, "User has uploaded a picture of a cat, not a .csv file"));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InformationLogNullThreadTest()
        {
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " INFO                         Levels of Low IQ detected", EventLogger.LogInfo(null, "Levels of Low IQ detected"));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InformationLogNullDetailsTest()
        {
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " INFO     GUI Thread           ", EventLogger.LogInfo(_guiThread.Name, ""));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void WarningLogNullThreadTest()
        {
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " WARNING                      Levels of Low IQ detected", EventLogger.LogWarning(null, "Levels of Low IQ detected"));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void WarningLogNullDetailsTest()
        {
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " WARNING  GUI Thread           ", EventLogger.LogWarning(_guiThread.Name, ""));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ErrorLogNullThreadTest()
        {
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " ERROR                        Levels of Low IQ detected", EventLogger.LogError(null, "Levels of Low IQ detected"));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ErrorLogNullDetailsTest()
        {
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " ERROR    GUI Thread           ", EventLogger.LogError(_guiThread.Name, ""));
        }

        [Test]
        public void MultiThreadTest()
        {
            _threadArray[0] = new Thread(EventLoggerTest.WriteError);
            _threadArray[0].Name = "Thread-1";

            _threadArray[1] = new Thread(EventLoggerTest.WriteError);
            _threadArray[1].Name = "Thread-2";

            _threadArray[2] = new Thread(EventLoggerTest.WriteError);
            _threadArray[2].Name = "Thread-3";

            _threadArray[3] = new Thread(EventLoggerTest.WriteError);
            _threadArray[3].Name = "Thread-4";

            for (int i = 0; i < 4; i++)
                _threadArray[i].Start();
    }

        public static void DummyDoWork(object data)
        {
            // Do nothing
        }

        public static void WriteError()
        {
            Assert.AreEqual(DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + " ERROR    " + Thread.CurrentThread.Name + "                   ", EventLogger.LogError(Thread.CurrentThread.Name, "Weow weow weow"));
        }
    }
}
