using System;
using System.Collections.Generic;
using System.IO;
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
        #region PrivateMembers
        private Thread _threadOne;
        private Thread _longThreadName;
        private Thread _guiThread;
        private Thread _threadZero;
        private Thread[] _threadArray = new Thread[4];
        #endregion

        #region TestSetup
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
        #endregion

        #region LogTestsOfEachType
        [Test]
        public void InformationLogTest()
        {
            Assert.AreEqual(DateTime.Now.ToString(EventLogger.TimeFormatString) + "    INFO       Thread-1                  Application started", EventLogger.LogInfo(null, _threadOne.Name, "Application started"));
            Assert.AreEqual(DateTime.Now.ToString(EventLogger.TimeFormatString) + "    INFO       Thread-1                  Loaded CSV File", EventLogger.LogInfo(null, _threadOne.Name, "Loaded CSV File"));
            Assert.AreEqual(DateTime.Now.ToString(EventLogger.TimeFormatString) + "    INFO       A_LONG_THREAD_NAME_IS_THI Loaded CSV File", EventLogger.LogInfo(null, _longThreadName.Name, "Loaded CSV File"));
        }

        [Test]
        public void WarningLogTest()
        {
            Assert.AreEqual(DateTime.Now.ToString(EventLogger.TimeFormatString) + "    WARNING    GUI Thread                Levels of Low IQ detected", EventLogger.LogWarning(null, _guiThread.Name, "Levels of Low IQ detected"));
            Assert.AreEqual(DateTime.Now.ToString(EventLogger.TimeFormatString) + "    WARNING    GUI Thread                Just another silly test", EventLogger.LogWarning(null, _guiThread.Name, "Just another silly test"));
        }

        [Test]
        public void ErrorLogTest()
        {
            Assert.AreEqual(DateTime.Now.ToString(EventLogger.TimeFormatString) + "    ERROR      Thread-0                  Application fatal error or something", EventLogger.LogError(null, _threadZero.Name, "Application fatal error or something"));
            Assert.AreEqual(DateTime.Now.ToString(EventLogger.TimeFormatString) + "    ERROR      Thread-0                  User has uploaded a picture of a cat, not a .csv file", EventLogger.LogError(null, _threadZero.Name, "User has uploaded a picture of a cat, not a .csv file"));
        }
        #endregion

        #region NullArguementTests
        [Test]
        public void InformationLogNullThreadTest()
        {
            Assert.AreEqual(DateTime.Now.ToString(EventLogger.TimeFormatString) + "    INFO       <No Thread Name>          Levels of Low IQ detected", EventLogger.LogInfo(null, null, "Levels of Low IQ detected"));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InformationLogNullDetailsTest()
        {
            Assert.AreEqual(DateTime.Now.ToString(EventLogger.TimeFormatString) + "    INFO       GUI Thread                ", EventLogger.LogInfo(null, _guiThread.Name, ""));
        }

        [Test]
        public void WarningLogNullThreadTest()
        {
            Assert.AreEqual(DateTime.Now.ToString(EventLogger.TimeFormatString) + "    WARNING    <No Thread Name>          Levels of Low IQ detected", EventLogger.LogWarning(null, null, "Levels of Low IQ detected"));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void WarningLogNullDetailsTest()
        {
            Assert.AreEqual(DateTime.Now.ToString(EventLogger.TimeFormatString) + "    WARNING    GUI Thread                ", EventLogger.LogWarning(null, _guiThread.Name, ""));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ErrorLogNullDetailsTest()
        {
            Assert.AreEqual(DateTime.Now.ToString(EventLogger.TimeFormatString) + "    ERROR      GUI Thread                ", EventLogger.LogError(null, _guiThread.Name, ""));
        }

        [Test]
        public void ErrorLogNullThreadTest()
        {
            Assert.AreEqual(DateTime.Now.ToString(EventLogger.TimeFormatString) + "    ERROR      <No Thread Name>          Levels of Low IQ detected", EventLogger.LogError(null, null, "Levels of Low IQ detected"));
        }
        #endregion

        #region ThreadTestsAndMethods
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
            int i = 9;
            i -= 9;
            //i /= 0;
        }

        public static void WriteError()
        {
            Assert.AreEqual(DateTime.Now.ToString(EventLogger.TimeFormatString) + " ERROR    " + Thread.CurrentThread.Name + "                   ", EventLogger.LogError(null, Thread.CurrentThread.Name, "Weow weow weow"));
        }
        #endregion

        #region Individual Sensor Logging Tests
        [Test]
        public void LogSensorInfoTest()
        {
            Assert.AreEqual(DateTime.Now.ToString(EventLogger.TimeFormatString) + "    INFO       Temperature               Because we can.", EventLogger.LogSensorInfo(null, "Temperature", "Because we can."));

            Assert.AreEqual(DateTime.Now.ToString(EventLogger.TimeFormatString) + "    INFO       This Sensor               Because we can.", EventLogger.LogSensorInfo(null, "This Sensor", "Because we can."));
        }

        [Test]
        public void LogSensorChangeTest()
        {
            var state = new SensorState(null, DateTime.Now,
                                        new Dictionary<DateTime, float> { { new DateTime(2011, 5, 5, 5, 5, 0), 2000 } }, null);
            state.Reason = new ChangeReason(0, "Because we can.");
            var result = state.LogChange("Temperature", "Extrapolation performed.");

            Assert.AreEqual(DateTime.Now.ToString(EventLogger.TimeFormatString) + "    INFO       Temperature               Extrapolation performed. Reason: [0] Because we can.", result);

            state = new SensorState(null, DateTime.Now,
                                        new Dictionary<DateTime, float> { { new DateTime(2011, 5, 5, 5, 5, 0), 2000 } }, null);
            state.Reason = new ChangeReason(0, "Because we can.");
            result = state.LogChange("Temperature", "Did some awesome work on the dataset.");

            Assert.AreEqual(DateTime.Now.ToString(EventLogger.TimeFormatString) + "    INFO       Temperature               Did some awesome work on the dataset. Reason: [0] Because we can.", result);
        }

        [Test]
        public void LogSensorChangeToFileTest()
        {
            var sensorLogPath = Path.Combine(EventLogger.GetSensorLogPath("Temperature"));
            var sensorTwoLogPath = Path.Combine(EventLogger.GetSensorLogPath("Temperature20"));

            if (File.Exists(sensorLogPath))
                File.Delete(sensorLogPath);

            if (File.Exists(sensorTwoLogPath))
                File.Delete(sensorTwoLogPath);

            var state = new SensorState(null, DateTime.Now,
                                        new Dictionary<DateTime, float> { { new DateTime(2011, 5, 5, 5, 5, 0), 2000 } }, null);
            state.Reason = new ChangeReason(0, "Because we can.");
            state.LogChange("Temperature", "Extrapolation performed.");

            Assert.AreEqual(DateTime.Now.ToString(EventLogger.TimeFormatString) + "    INFO       Temperature               Extrapolation performed. Reason: [0] Because we can.\r\n", File.ReadAllText(sensorLogPath));

            state = new SensorState(null, DateTime.Now,
                                        new Dictionary<DateTime, float> { { new DateTime(2011, 5, 5, 5, 5, 0), 2000 } }, null);
            state.Reason = new ChangeReason(0, "Because we can.");
            state.LogChange("Temperature20", "Extrapolation performed.");

            Assert.AreEqual(DateTime.Now.ToString(EventLogger.TimeFormatString) + "    INFO       Temperature20             Extrapolation performed. Reason: [0] Because we can.\r\n", File.ReadAllText(sensorTwoLogPath));

            Assert.IsTrue(File.Exists(sensorLogPath));
            Assert.IsTrue(File.Exists(sensorTwoLogPath));
        }
        #endregion
    }
}
