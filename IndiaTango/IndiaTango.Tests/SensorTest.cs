using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using IndiaTango.Models;

namespace IndiaTango.Tests
{
    [TestFixture]
    class SensorTest
    {
        private Stack<SensorState> _testUndoStack;
        private Stack<SensorState> _secondUndoStack;
        private Stack<SensorState> _blankStack = new Stack<SensorState>();

        [SetUp]
        public void Setup()
        {
            _testUndoStack = new Stack<SensorState>();

            var testState = new SensorState(new DateTime(2010, 5, 7, 18, 42, 0));
            var secondaryTestState = new SensorState(new DateTime(2010, 12, 18, 5, 22, 0));

            _testUndoStack.Push(testState);
            _testUndoStack.Push(secondaryTestState);

            _secondUndoStack = new Stack<SensorState>();
            _secondUndoStack.Push(new SensorState(new DateTime(2005, 4, 3, 2, 1, 0)));
            _secondUndoStack.Push(new SensorState(new DateTime(2005, 4, 4, 2, 1, 0)));
        }

        #region Undo Stack Tests
        [Test]
        public void GetUndoStack()
        {
            var testSensor = new Sensor(_testUndoStack, _blankStack);

            Assert.AreEqual(_testUndoStack, testSensor.UndoStack);
        }

        [Test]
        public void SetUndoStack()
        {
            var testSensor = new Sensor(_testUndoStack, _blankStack);

            testSensor.UndoStack = _secondUndoStack;
            Assert.AreEqual(_secondUndoStack, testSensor.UndoStack);

            testSensor.UndoStack = _testUndoStack;
            Assert.AreEqual(_testUndoStack, testSensor.UndoStack);
        }
        #endregion

        #region Redo Stack Tests
        [Test]
        public void GetRedoStack()
        {
            var testSensor = new Sensor(_testUndoStack, _secondUndoStack);

            Assert.AreEqual(_secondUndoStack, testSensor.RedoStack);
        }

        [Test]
        public void SetRedoStack()
        {
            var testSensor = new Sensor(_testUndoStack, _secondUndoStack);
            testSensor.RedoStack = _testUndoStack;

            Assert.AreEqual(_testUndoStack, testSensor.RedoStack);

            testSensor.RedoStack = _secondUndoStack;
            
            Assert.AreEqual(_secondUndoStack, testSensor.RedoStack);
            // TODO: test values with Equals
        }
        #endregion

        #region Construction Tests
        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullRedoButNotUndoStack()
        {
            var testSensor = new Sensor(_testUndoStack, null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullUndoAndRedoStack()
        {
            var testSensor = new Sensor(null, null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullUndoButNotRedoStack()
        {
            var testSensor = new Sensor(null, _testUndoStack);
        }

        [Test]
        public void NoParameterConstructor()
        {
            var testSensor = new Sensor();

            Assert.IsNotNull(testSensor.UndoStack);
            Assert.IsNotNull(testSensor.RedoStack);
            Assert.IsNotNull(testSensor.CalibrationDates);
            Assert.IsTrue(testSensor.UndoStack.Count == 0 && testSensor.RedoStack.Count == 0 && testSensor.CalibrationDates.Count == 0);
        }
        #endregion

        #region Calibration Dates Tests
        [Test]
        public void GetCalibrationDates()
        {
            List<DateTime> calibrationDatesTest = new List<DateTime>();
            calibrationDatesTest.Add(new DateTime(2011, 7, 4, 21, 15, 0));
            calibrationDatesTest.Add(new DateTime(2011, 7, 4, 21, 30, 0));
            calibrationDatesTest.Add(new DateTime(2011, 7, 4, 21, 45, 0));
            calibrationDatesTest.Add(new DateTime(2011, 7, 4, 22, 0, 0));

            Sensor testSensor = new Sensor(_testUndoStack, _secondUndoStack, calibrationDatesTest);

            Assert.AreEqual(calibrationDatesTest, testSensor.CalibrationDates);
        }

        [Test]
        public void SetCalibrationDates()
        {
            List<DateTime> calibrationDatesTest = new List<DateTime>();
            calibrationDatesTest.Add(new DateTime(2011, 7, 4, 21, 15, 0));
            calibrationDatesTest.Add(new DateTime(2011, 7, 4, 21, 30, 0));
            calibrationDatesTest.Add(new DateTime(2011, 7, 4, 21, 45, 0));
            calibrationDatesTest.Add(new DateTime(2011, 7, 4, 22, 0, 0));

            Sensor testSensor = new Sensor(_testUndoStack, _secondUndoStack, calibrationDatesTest);

            List<DateTime> secondCalibrationTest = new List<DateTime>();
            secondCalibrationTest.Add(new DateTime(2010, 7, 3, 21, 15, 0));
            secondCalibrationTest.Add(new DateTime(2010, 7, 3, 21, 30, 0));
            secondCalibrationTest.Add(new DateTime(2010, 7, 3, 21, 45, 0));
            secondCalibrationTest.Add(new DateTime(2010, 7, 3, 22, 0, 0));

            testSensor.CalibrationDates = secondCalibrationTest;

            Assert.AreEqual(secondCalibrationTest, testSensor.CalibrationDates);
        }
        #endregion
    }
}
