﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IndiaTango.Models;
using NUnit.Framework;

namespace IndiaTango.Tests
{
    [TestFixture]
    class SensorTest
    {
        #region Private Testing Fields
        private string _testName = "Joe Bloggs";
        private string _testDescription = "Temperature Sensor";
        private float _testUpperLimit = 50;
        private float _testLowerLimit = 10;
        private string _testUnit = "C";
        private float _testMaxRateOfChange = 2;
        private string _testManufacturer = "Synergy Corp";
        private string _testSerial = "XX3323211";

        private Stack<SensorState> _testUndoStates;
        private Stack<SensorState> _secondUndoStates;
        private Stack<SensorState> _blankStack = new Stack<SensorState>();
        private List<DateTime> _blankCalibrationDates = new List<DateTime>();

        private Dataset _ds = null;
        private Dataset _ds2 = null;

        private Contact contact = new Contact("Steven", "McTainsh", "smctainsh@gmail.com", "Test", "123213213");

        private Sensor _sensor1;
        private Sensor _sensor2;
        private Sensor _sensor3;
        private Sensor _undoSensor;
        private Sensor _secondUndoSensor;
        private Sensor _sensorEmpty;
        private Sensor _tempSensor;
        #endregion

        [SetUp]
        public void Setup()
        {
            _testUndoStates = new Stack<SensorState>();

            var testState = new SensorState(new DateTime(2010, 5, 7, 18, 42, 0));
            var secondaryTestState = new SensorState(new DateTime(2010, 12, 18, 5, 22, 0));

            _testUndoStates.Push(testState);
            _testUndoStates.Push(secondaryTestState);

            _secondUndoStates = new Stack<SensorState>();
            _secondUndoStates.Push(new SensorState(new DateTime(2005, 4, 3, 2, 1, 0)));
            _secondUndoStates.Push(new SensorState(new DateTime(2005, 4, 4, 2, 1, 0)));

            

            _tempSensor = new Sensor("Temperature", "C");

            // Initialise sensors for undo testing
            _undoSensor = new Sensor("Temperature", "C");
            _undoSensor.AddState(new SensorState(new DateTime(2011, 8, 11),
                                                       new Dictionary<DateTime, float>{{new DateTime(2011, 8, 12), 66.77f}}));
            _undoSensor.AddState(new SensorState(new DateTime(2011, 8, 12),
                                            new Dictionary<DateTime, float>{{new DateTime(2011, 8, 12), 66.77f}}));

            _secondUndoSensor = new Sensor("Temperature", "C");
            _secondUndoSensor.AddState(new SensorState(new DateTime(2011, 3, 11),
                                                       new Dictionary<DateTime, float>{{ new DateTime(2011, 8, 12), 66.77f} }));
            _secondUndoSensor.AddState(new SensorState(new DateTime(2011, 3, 12),
                                            new Dictionary<DateTime, float>{{new DateTime(2011, 8, 12), 66.77f}}));

            _sensorEmpty = new Sensor("Temperature", "C");
            _ds = new Dataset(new Site(10, "Lake", "Bob Smith", contact, contact, contact, new GPSCoords(50, 50)));
            _ds2 = new Dataset(new Site(10, "Lake Awesome", "Andy Smith", contact, contact, contact, new GPSCoords(70, 30)));

            _sensor1 = new Sensor("Temperature", "Temperature at 10m", 100, 20, "°C", 0.003f, "Awesome Industries", _testSerial, _ds);
            _sensor2 = new Sensor("DO", "Dissolved Oxygen in the water", 50, 0, "%", 5.6f, "SensorPlus", "SENSORPLUS123120A000", _ds2);
        }

        #region Undo Stack Tests
        [Test]
        public void GetUndoStates()
        {
            var testSensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit, _testMaxRateOfChange, _testManufacturer, _testSerial, _testUndoStates, _blankStack, _blankCalibrationDates, _ds);

            Assert.AreEqual(_testUndoStates, testSensor.UndoStates);
        }

        [Test]
        public void SetUndoStates()
        {
            var testSensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit, _testMaxRateOfChange, _testManufacturer, _testSerial, new Stack<SensorState>(), _blankStack, _blankCalibrationDates, _ds);

            var secondStates = _secondUndoStates.Reverse();

            foreach(SensorState s in secondStates)
                testSensor.AddState(s);

            Assert.AreEqual(_secondUndoStates, testSensor.UndoStates);

            // Reset
            testSensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit, _testMaxRateOfChange, _testManufacturer, _testSerial, _testUndoStates, _blankStack, _blankCalibrationDates, _ds);

            var testUndo = _testUndoStates.Reverse();

            foreach (SensorState s in testUndo)
                testSensor.AddState(s);

            Assert.AreEqual(_testUndoStates, testSensor.UndoStates);
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void SetCalibrationDatesListToNull()
        {
            var testSensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit, _testMaxRateOfChange, _testManufacturer, _testSerial, _testUndoStates, _blankStack, _blankCalibrationDates, _ds);
            testSensor.CalibrationDates = null;
        }
        #endregion

        #region Redo Stack Tests
        [Test]
        public void GetRedoStates()
        {
            var testSensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit, _testMaxRateOfChange, _testManufacturer, _testSerial, _testUndoStates, _secondUndoStates, _blankCalibrationDates, _ds);

            Assert.AreEqual(_secondUndoStates, testSensor.RedoStates);
        }
        #endregion

        #region Construction Tests
        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullRedoButNotUndoStates()
        {
            var testSensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit, _testMaxRateOfChange, _testManufacturer, _testSerial, _testUndoStates, null, _blankCalibrationDates, _ds);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullUndoAndRedoStates()
        {
            var testSensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit, _testMaxRateOfChange, _testManufacturer, _testSerial, null, null, _blankCalibrationDates, _ds);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullUndoButNotRedoStates()
        {
            var testSensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit, _testMaxRateOfChange, _testManufacturer, _testSerial, null, _testUndoStates, _blankCalibrationDates, _ds);
        }

        [Test]
        public void MinimalConstructorsCreateEmptyStacksAndLists()
        {
            var testSensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit, _testMaxRateOfChange, _testManufacturer, _testSerial, _ds);

            Assert.IsNotNull(testSensor.UndoStates);
            Assert.IsNotNull(testSensor.RedoStates);
            Assert.IsNotNull(testSensor.CalibrationDates);
            Assert.IsTrue(testSensor.UndoStates.Count == 0 && testSensor.RedoStates.Count == 0 && testSensor.CalibrationDates.Count == 0);

            testSensor = new Sensor(_testName, _testUnit);

            Assert.IsNotNull(testSensor.UndoStates);
            Assert.IsNotNull(testSensor.RedoStates);
            Assert.IsNotNull(testSensor.CalibrationDates);
            Assert.IsTrue(testSensor.UndoStates.Count == 0 && testSensor.RedoStates.Count == 0 && testSensor.CalibrationDates.Count == 0);
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

            Sensor testSensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit, _testMaxRateOfChange, _testManufacturer, _testSerial, _testUndoStates, _secondUndoStates, calibrationDatesTest, _ds);

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

            Sensor testSensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit, _testMaxRateOfChange, _testManufacturer, _testSerial, _testUndoStates, _secondUndoStates, calibrationDatesTest, _ds);

            List<DateTime> secondCalibrationTest = new List<DateTime>();
            secondCalibrationTest.Add(new DateTime(2010, 7, 3, 21, 15, 0));
            secondCalibrationTest.Add(new DateTime(2010, 7, 3, 21, 30, 0));
            secondCalibrationTest.Add(new DateTime(2010, 7, 3, 21, 45, 0));
            secondCalibrationTest.Add(new DateTime(2010, 7, 3, 22, 0, 0));

            testSensor.CalibrationDates = secondCalibrationTest;

            Assert.AreEqual(secondCalibrationTest, testSensor.CalibrationDates);
        }
        #endregion

        #region Getter Tests
        [Test]
        public void GetNameTest()
        {
            Assert.AreEqual("Temperature", _sensor1.Name);
            Assert.AreEqual("DO", _sensor2.Name);
        }

        [Test]
        public void GetDescriptionTest()
        {
            Assert.AreEqual("Temperature at 10m",_sensor1.Description);
            Assert.AreEqual("Dissolved Oxygen in the water", _sensor2.Description);
        }

        [Test]
        public void GetUpperLimitTest()
        {
            Assert.AreEqual(100,_sensor1.UpperLimit);
            Assert.AreEqual(50, _sensor2.UpperLimit);
        }

        [Test]
        public void GetLowerLimitTest()
        {
            Assert.AreEqual(20,_sensor1.LowerLimit);
            Assert.AreEqual(0, _sensor2.LowerLimit);
        }

        [Test]
        public void GetUnitTest()
        {
            Assert.AreEqual("°C",_sensor1.Unit);
            Assert.AreEqual("%", _sensor2.Unit);
        }

        [Test]
        public void GetMaxRateOfChangeTest()
        {
            Assert.AreEqual(0.003f,_sensor1.MaxRateOfChange);
            Assert.AreEqual(5.6f, _sensor2.MaxRateOfChange);
        }
       
        [Test]
        public void GetManufacturerTest()
        {
            Assert.AreEqual("Awesome Industries", _sensor1.Manufacturer);
        }

        [Test]
        public void GetSerialNumberTest()
        {
            Assert.AreEqual("XX3323211", _sensor1.SerialNumber);
            Assert.AreEqual("SENSORPLUS123120A000", _sensor2.SerialNumber);
        }
        #endregion

        #region Setter Tests
        [Test]
        public void SetNameTest()
        {
            _sensor1.Name = "Humidity";
            Assert.AreEqual("Humidity",_sensor1.Name);

            _sensor1.Name = "Rainfall";
            Assert.AreEqual("Rainfall", _sensor1.Name);
        }

        [Test]
        public void SetUpperLimitTest()
        {
            _sensor1.UpperLimit = 120;
            Assert.AreEqual(120,_sensor1.UpperLimit);

            _sensor2.UpperLimit = 45;
            Assert.AreEqual(45, _sensor2.UpperLimit);
        }

        [Test]
        public void SetLowerLimitTest()
        {
            _sensor1.LowerLimit = 0;
            Assert.AreEqual(0, _sensor1.LowerLimit);

            _sensor2.LowerLimit = 5;
            Assert.AreEqual(5, _sensor2.LowerLimit);
        }

        [Test]
        public void SetLowerUnitTest()
        {
            _sensor1.Unit = "°F";
            Assert.AreEqual("°F", _sensor1.Unit);

            _sensor2.Unit = "$";
            Assert.AreEqual("$", _sensor2.Unit);
        }

        [Test]
        public void SetMaxRateOfChangeTest()
        {
            _sensor1.MaxRateOfChange = 0.03f;
            Assert.AreEqual(0.03f, _sensor1.MaxRateOfChange);

            _sensor2.MaxRateOfChange = 6f;
            Assert.AreEqual(6f, _sensor2.MaxRateOfChange);
        }

        [Test]
        public void SetManufacturerTest()
        {
            _sensor1.SerialNumber = "BB329AFAJSLK";
            Assert.AreEqual(_sensor1.SerialNumber, "BB329AFAJSLK");

            _sensor1.SerialNumber = "";
            Assert.AreEqual(_sensor1.SerialNumber, "");
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SetSerialNumberNullTest()
        {
            _sensor1.SerialNumber = null;
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SetSerialNumberNullConstructorTest()
        {
            new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit, _testMaxRateOfChange,
                       _testManufacturer, null, _ds);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void SetLowerLimitGreaterThanUpperLimit()
        {
            _sensor1.LowerLimit = 120;
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void SetUpperLimitLowerThanLowerLimit()
        {
            _sensor1.UpperLimit = 0;
        }
        #endregion

        #region Invalid Values Tests
        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InvalidNameTest()
        {
            _sensor3 = new Sensor("", "", 0, 0, "%", 0, "", "", _ds);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InvalidUnitTest()
        {
            _sensor3 = new Sensor("Temperature", "", 0, 0, "", 0, "", "", _ds);
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void InvalidSetNameTest()
        {
            _sensor1.Name = "";
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void InvalidSetUnitTest()
        {
            _sensor1.Unit = "";
        }
        #endregion

        #region Undo Method Tests
        [Test]
        public void ValidUndoStatesAfterUndo()
        {
            var correctStack = new Stack<SensorState>();
            correctStack.Push(new SensorState(new DateTime(2011, 8, 11),
                                           new Dictionary<DateTime, float>{{new DateTime(2011, 8, 12), 66.77f}}));

            _undoSensor.Undo();
            Assert.AreEqual(correctStack, _undoSensor.UndoStates);

            var correctStackTwo = new Stack<SensorState>();
            correctStackTwo.Push(new SensorState(new DateTime(2011, 3, 11),
                                           new Dictionary<DateTime, float>{{ new DateTime(2011, 8, 12), 66.77f} }));
            _secondUndoSensor.Undo();
            Assert.AreEqual(correctStackTwo, _secondUndoSensor.UndoStates);
        }

        [Test]
        public void ValidRedoStatesAfterUndo()
        {
            var correctStack = new Stack<SensorState>();
            correctStack.Push(new SensorState(new DateTime(2011, 8, 12),
                                           new Dictionary<DateTime, float>{{new DateTime(2011, 8, 12), 66.77f}}));
            correctStack.Push(new SensorState(new DateTime(2011, 8, 13),
                                           new Dictionary<DateTime, float> { { new DateTime(2011, 8, 12), 66.77f } }));

            _undoSensor.Undo();
            Assert.AreEqual(correctStack.ElementAt(1), _undoSensor.RedoStates[0]);

            var correctStackTwo = new Stack<SensorState>();
            correctStackTwo.Push(new SensorState(new DateTime(2011, 3, 12),
                                           new Dictionary<DateTime, float>{{new DateTime(2011, 8, 12), 66.77f}}));
            correctStackTwo.Push(new SensorState(new DateTime(2011, 3, 15),
                               new Dictionary<DateTime, float> { { new DateTime(2011, 8, 12), 66.77f } }));

            _secondUndoSensor.Undo();
            Assert.AreEqual(correctStackTwo.ElementAt(1), _secondUndoSensor.RedoStates[0]);
        }

        [Test]
        public void ValidUndoStatesAfterRedo()
        {
            var redoSensor = new Sensor("Temperature", "C");

            var correctStackItem = new SensorState(new DateTime(2011, 7, 5, 22, 47, 0), new Dictionary<DateTime, float> { { new DateTime(2010, 5, 5, 22, 00, 0), 22.5f }, { new DateTime(2010, 5, 5, 22, 15, 0), 21.4f }, { new DateTime(2010, 5, 5, 22, 30, 0), 22.0f } });
            var correctStackItemTwo = new SensorState(new DateTime(2011, 7, 9, 22, 47, 0), new Dictionary<DateTime, float> { { new DateTime(2010, 5, 5, 22, 00, 0), 22.5f }, { new DateTime(2010, 5, 5, 22, 15, 0), 21.4f }, { new DateTime(2010, 5, 5, 22, 30, 0), 22.0f } });

            redoSensor.AddState(correctStackItem);
            redoSensor.AddState(correctStackItemTwo);

            redoSensor.Undo();

            Assert.AreEqual(correctStackItem, redoSensor.UndoStates[0]);

            redoSensor.Redo();

            Assert.AreEqual(2, redoSensor.UndoStates.Count);
        }

        [Test]
        public void ValidRedoStatesAfterRedo()
        {
            var redoSensor = new Sensor("Temperature", "C");

            var correctStackItem = new SensorState(new DateTime(2011, 7, 5, 22, 47, 0), new Dictionary<DateTime, float> { { new DateTime(2010, 5, 5, 22, 00, 0), 22.5f }, { new DateTime(2010, 5, 5, 22, 15, 0), 21.4f }, { new DateTime(2010, 5, 5, 22, 30, 0), 22.0f } });

            redoSensor.AddState(correctStackItem);
            redoSensor.AddState(correctStackItem);

            redoSensor.Undo();

            Assert.AreEqual(1, redoSensor.RedoStates.Count);

            redoSensor.Redo();

            Assert.AreEqual(0, redoSensor.RedoStates.Count);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage="Undo is not possible at this stage. There are no more possible states to undo to.")]
        public void ExceptionWhenUndoNotPossible()
        {
            _undoSensor.Undo();
            _undoSensor.Undo(); // Should trigger the exception
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Redo is not possible at this stage. There are no more possible states to redo to.")]
        public void ExceptionWhenRedoNotPossible()
        {
            // Undo stack has to have at least one element on it (current state)
            _undoSensor.Undo();

            _undoSensor.Redo();
            _undoSensor.Redo(); // Should trigger the exception
        }

        [Test]
        public void AddStateMaximumFiveEntries()
        {
            var s = new Sensor("Temperature", "C");
            s.AddState(new SensorState(DateTime.Now));
            s.AddState(new SensorState(DateTime.Now));
            s.AddState(new SensorState(DateTime.Now));
            s.AddState(new SensorState(DateTime.Now));
            s.AddState(new SensorState(DateTime.Now));
            s.AddState(new SensorState(DateTime.Now));
            s.AddState(new SensorState(DateTime.Now));

            Assert.AreEqual(5, s.UndoStates.Count);
        }

        [Test]
        public void UndoRedoStackMaximumFiveEntries()
        {
            var s = new Sensor("Temperature", "C");

            s.AddState(new SensorState(DateTime.Now));
            s.AddState(new SensorState(DateTime.Now));
            s.AddState(new SensorState(DateTime.Now));
            s.AddState(new SensorState(DateTime.Now));
            s.AddState(new SensorState(DateTime.Now));
            s.AddState(new SensorState(DateTime.Now));
            s.AddState(new SensorState(DateTime.Now));

            Assert.AreEqual(5, s.UndoStates.Count);
            Assert.AreEqual(0, s.RedoStates.Count);

            s.Undo();
            s.Undo();
            s.Undo();
            s.Undo();

            // Since the top of the undo stack is the current state, we must have at least one item left on the undo stack...
            Assert.AreEqual(1, s.UndoStates.Count); // This is the real test
            Assert.AreEqual(4, s.RedoStates.Count);

            s.Redo();
            s.Redo();
            s.Redo();
            s.Redo();

            Assert.AreEqual(5, s.UndoStates.Count); // This is the real test
            Assert.AreEqual(0, s.RedoStates.Count);
        }

        [Test]
        public void UndoStackPushesCorrectEntriesAndMaxFive()
        {
            var s = new Sensor("Temperature", "C");
            var baseDate = new DateTime(2011, 7, 7, 12, 15, 0);

            s.AddState(new SensorState(baseDate));
            Assert.AreEqual(baseDate, s.UndoStates[0].EditTimestamp);

            s.AddState(new SensorState(baseDate.AddMinutes(15)));
            Assert.AreEqual(baseDate.AddMinutes(15), s.UndoStates[0].EditTimestamp);

            s.AddState(new SensorState(baseDate.AddMinutes(30)));
            Assert.AreEqual(baseDate.AddMinutes(30), s.UndoStates[0].EditTimestamp);

            s.AddState(new SensorState(baseDate.AddMinutes(45)));
            Assert.AreEqual(baseDate.AddMinutes(45), s.UndoStates[0].EditTimestamp);

            s.AddState(new SensorState(baseDate.AddMinutes(60)));
            Assert.AreEqual(baseDate.AddMinutes(60), s.UndoStates[0].EditTimestamp);

            // Now, for the real test of the method...
            s.AddState(new SensorState(baseDate.AddMinutes(75)));
            Assert.AreEqual(baseDate.AddMinutes(75), s.UndoStates[0].EditTimestamp);
            Assert.AreEqual(baseDate.AddMinutes(15), s.UndoStates[4].EditTimestamp);

            s.AddState(new SensorState(baseDate.AddMinutes(90)));
            Assert.AreEqual(baseDate.AddMinutes(90), s.UndoStates[0].EditTimestamp);
            Assert.AreEqual(baseDate.AddMinutes(30), s.UndoStates[4].EditTimestamp);
        }

        [Test]
        public void UndoToParticularState()
        {
            var s = new Sensor("Temperature", "C");
            var baseDate = new DateTime(2011, 7, 7, 12, 15, 0);

            s.AddState(new SensorState(baseDate));
            s.AddState(new SensorState(baseDate.AddMinutes(15)));
            s.AddState(new SensorState(baseDate.AddMinutes(30)));
            s.AddState(new SensorState(baseDate.AddMinutes(45)));
            s.AddState(new SensorState(baseDate.AddMinutes(60)));
            s.AddState(new SensorState(baseDate.AddMinutes(75)));
            s.AddState(new SensorState(baseDate.AddMinutes(90)));
            s.AddState(new SensorState(baseDate.AddMinutes(105)));

            s.Undo(baseDate.AddMinutes(75));

            Assert.AreEqual(2, s.RedoStates.Count);
            Assert.AreEqual(baseDate.AddMinutes(90), s.RedoStates[0].EditTimestamp);
            Assert.AreEqual(baseDate.AddMinutes(105), s.RedoStates[1].EditTimestamp);
        }

        [Test]
        public void RedoToParticularState()
        {
            var s = new Sensor("Temperature", "C");
            var baseDate = new DateTime(2011, 7, 7, 12, 15, 0);

            s.AddState(new SensorState(baseDate));
            s.AddState(new SensorState(baseDate.AddMinutes(15)));
            s.AddState(new SensorState(baseDate.AddMinutes(30)));
            s.AddState(new SensorState(baseDate.AddMinutes(45)));
            s.AddState(new SensorState(baseDate.AddMinutes(60)));
            s.AddState(new SensorState(baseDate.AddMinutes(75)));
            s.AddState(new SensorState(baseDate.AddMinutes(90)));
            s.AddState(new SensorState(baseDate.AddMinutes(105)));

            s.Undo(baseDate.AddMinutes(60)); // Redo stack - 105, 90, 75

            s.Redo(baseDate.AddMinutes(90));

            Assert.AreEqual(1, s.RedoStates.Count);
            Assert.AreEqual(baseDate.AddMinutes(105), s.RedoStates[0].EditTimestamp);

            s.Redo(baseDate.AddMinutes(105));

            Assert.AreEqual(0, s.RedoStates.Count);
        }

        #endregion

        #region Multi Level Undo Tests
        [Test]
        public void ValidUndoStatesAfterMultilevelUndo()
        {
            // Undo twice to get empty stack in this case
            _undoSensor.AddState(new SensorState(DateTime.Now));

            _undoSensor.Undo();
            _undoSensor.Undo();

            Assert.AreEqual(1, _undoSensor.UndoStates.Count);
        }

        [Test]
        public void ValidRedoStatesAfterMultilevelUndo()
        {
            var correctRedoStates = new Stack<SensorState>();
            correctRedoStates.Push(new SensorState(new DateTime(2011, 8, 12),
                                                       new Dictionary<DateTime, float> {{new DateTime(2011, 8, 12), 66.77f}}));
            correctRedoStates.Push(new SensorState(new DateTime(2011, 8, 11),
                                            new Dictionary<DateTime, float>{{new DateTime(2011, 8, 12), 66.77f}}));

            _undoSensor.Undo(); // At least 1 item must be on undo stack (current state)

            Assert.AreEqual(correctRedoStates.ElementAt(1), _undoSensor.RedoStates[0]);
        }
        #endregion

        #region Multi Level Redo Tests
        [Test]
        public void ValidUndoStatesAfterMultilevelRedo()
        {
            _undoSensor = new Sensor("Temperature", "C");

            var correctStack = new Stack<SensorState>();
            correctStack.Push(new SensorState(new DateTime(2011, 8, 12),
                                                       new Dictionary<DateTime, float>{{new DateTime(2011, 8, 12), 66.77f}}));
            correctStack.Push(new SensorState(new DateTime(2011, 8, 11),
                                            new Dictionary<DateTime, float>{{new DateTime(2011, 8, 12), 66.77f}}));
            correctStack.Push(new SensorState(new DateTime(2011, 8, 12),
                                new Dictionary<DateTime, float> { { new DateTime(2011, 8, 12), 64.77f } }));

            _undoSensor.AddState(new SensorState(new DateTime(2011, 8, 11),
                                                       new Dictionary<DateTime, float>{{new DateTime(2011, 8, 12), 66.77f}}));
            _undoSensor.AddState(new SensorState(new DateTime(2011, 8, 12),
                                            new Dictionary<DateTime, float>{{new DateTime(2011, 8, 12), 66.77f}}));
            _undoSensor.AddState(new SensorState(new DateTime(2011, 8, 12),
                                new Dictionary<DateTime, float> { { new DateTime(2011, 8, 12), 64.77f } }));

            _undoSensor.Undo();
            _undoSensor.Undo();

            Assert.AreEqual(2, _undoSensor.RedoStates.Count);
            Assert.AreEqual(1, _undoSensor.UndoStates.Count);

            _undoSensor.Redo();
            _undoSensor.Redo();

            Assert.AreEqual(0, _undoSensor.RedoStates.Count);
            Assert.AreEqual(3, _undoSensor.UndoStates.Count);
            
        }
        #endregion

        #region Current State Tests
        [Test]
        public void AddStateTest()
        {
            _testUndoStates.Clear();
            _testUndoStates.Push(new SensorState(DateTime.Now));
            _sensor1.AddState(new SensorState(DateTime.Now));

            Assert.AreEqual(_testUndoStates.Peek(), _sensor1.CurrentState);
            Assert.AreEqual(_testUndoStates, _sensor1.UndoStates);
            Assert.IsEmpty(_sensor1.RedoStates);
        }
#endregion

        #region Sensor Error Threshold Tests
        [Test]
        public void GetAcceptableSensorThresholds()
        {
            var sensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit,
                                    _testMaxRateOfChange, _testManufacturer, _testSerial, _testUndoStates, _testUndoStates,
                                    new List<DateTime>(), 4, _ds);
            Assert.AreEqual(4, sensor.ErrorThreshold);

            var sensorTwo = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit,
                                    _testMaxRateOfChange, _testManufacturer, _testSerial, _testUndoStates, _testUndoStates,
                                    new List<DateTime>(), 6, _ds);
            Assert.AreEqual(6, sensorTwo.ErrorThreshold);
        }

        [Test]
        public void SetAcceptableSensorThresholds()
        {
            _tempSensor.ErrorThreshold = 6;
            Assert.AreEqual(6, _tempSensor.ErrorThreshold);

            _tempSensor.ErrorThreshold = 4;
            Assert.AreEqual(4, _tempSensor.ErrorThreshold);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void ConstructWithZeroErrorThreshold()
        {
            var sensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit,
                                     _testMaxRateOfChange, _testManufacturer, _testSerial, _testUndoStates, _testUndoStates,
                                     new List<DateTime>(), 0, _ds);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void ConstructWithNegativeErrorThreshold()
        {
             var sensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit,
                                     _testMaxRateOfChange, _testManufacturer, _testSerial, _testUndoStates, _testUndoStates,
                                     new List<DateTime>(), -9, _ds);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void SetNegativeSensorThreshold()
        {
            _tempSensor.ErrorThreshold = -8;
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void SetZeroSensorThreshold()
        {
            _tempSensor.ErrorThreshold = 0;
        }

        [Test]
        [ExpectedException(typeof(NullReferenceException))]
        public void NullSensorStateWithOperation()
        {
            var dataset =
                new Dataset(new Site(50, "Lake Rotorua", "Bob Smith", contact, contact, contact, new GPSCoords(50, 50)), new List<Sensor> { { _sensor1 } });
            var t = _sensor1.IsFailing(dataset);
        }

        [Test]
        [ExpectedException(typeof(NullReferenceException))]
        public void NullDatasetPassedToIsFailing()
        {
            var t = _sensor1.IsFailing(null);
        }
        #endregion

        #region Limit Sanity Checks
        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void UpperLimitLessThanLowerLimit()
        {
            var s = new Sensor("Awesome Sensor", "An awesome sensor", 10, 100, "A", 2, "Awesome Co", "XX323929", _ds);
        }
        #endregion

        #region Listed Sensor Tests
        [Test]
        public void GetSensorTest()
        {
            var ls = new ListedSensor(_sensor1, _ds);
            Assert.AreEqual(ls.Sensor.Name, "Temperature");

            var nls = new ListedSensor(_sensor2, _ds);
            Assert.AreEqual(nls.Sensor.Name, "DO");
        }

        [Test]
        public void SetSensorTest()
        {
            var ls = new ListedSensor(_sensor1, _ds);

            ls.Sensor = _sensor2;
            Assert.AreEqual(ls.Sensor.Name, "DO");

            ls.Sensor = _sensor1;
            Assert.AreEqual(ls.Sensor.Name, "Temperature");
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullConstructSensorTest()
        {
            var ls = new ListedSensor(null, _ds);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullSensorTest()
        {
            var ls = new ListedSensor(_sensor1, _ds);
            ls.Sensor = null;
        }

        [Test]
        public void GetDatasetTest()
        {
            var ls = new ListedSensor(_sensor1, _ds);
            Assert.AreEqual(ls.Dataset, _ds);

            var nls = new ListedSensor(_sensor1, _ds2);
            Assert.AreEqual(nls.Dataset, _ds2);
        }

        [Test]
        public void SetDatasetTest()
        {
            var ls = new ListedSensor(_sensor1, _ds);

            ls.Dataset = _ds2;
            Assert.AreEqual(ls.Dataset, _ds2);

            ls.Dataset = _ds;
            Assert.AreEqual(ls.Dataset, _ds);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullConstructDatasetTest()
        {
            var ls = new ListedSensor(_sensor1, null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullDatasetTest()
        {
            var ls = new ListedSensor(_sensor1, _ds);
            ls.Dataset = null;
        }
        #endregion

        #region Dataset Tests

        [Test]
        public void CheckDataSet()
        {
            Assert.AreEqual(_ds, _sensor1.Owner);
            Assert.AreNotEqual(_ds2,_sensor1.Owner);
            Assert.AreEqual(_ds2, _sensor2.Owner);
            Assert.AreNotEqual(_ds, _sensor2.Owner);
        }

        #endregion

        #region Revert To Raw Tests
        [Test]
        public void SetRawData()
        {
            var s = new Sensor("Temperature", "C");
            var baseDate = new DateTime(2011, 5, 5, 12, 15, 0);
            var original = new SensorState(new Dictionary<DateTime, float>() { { baseDate.AddMinutes(15), 20 }, { baseDate.AddMinutes(30), 40 }, { baseDate.AddMinutes(45), 60 }, { baseDate.AddMinutes(60), 80 } });

            s.RawData = original;

            Assert.AreEqual(s.RawData, original);
        }

        [Test]
        public void RevertToRaw()
        {
            var s = new Sensor("Temperature", "C");
            var baseDate = new DateTime(2011, 5, 5, 12, 15, 0);
            var original = new SensorState(new Dictionary<DateTime, float>() { { baseDate.AddMinutes(15), 20 }, { baseDate.AddMinutes(30), 40 }, { baseDate.AddMinutes(45), 60 }, { baseDate.AddMinutes(60), 80 } });

            var newValues = new SensorState(new Dictionary<DateTime, float>() { { baseDate.AddMinutes(15), 20 }, { baseDate.AddMinutes(30), 40 }, { baseDate.AddMinutes(45), 60 }, { baseDate.AddMinutes(60), 1000 } });

            s.RawData = original;

            s.AddState(newValues);
            s.RevertToRaw();

            Assert.AreEqual(s.CurrentState, original);
        }
        #endregion

        #region RawNameTests

        [Test]
        public void EnsureRawNameNeverChanges()
        {
            var rawName = _sensor1.RawName;
            Assert.AreEqual(rawName, _sensor1.Name);

            _sensor1.Name = "Testing";

            Assert.AreNotEqual(_sensor1.Name, _sensor1.RawName);
            Assert.AreEqual(rawName, _sensor1.RawName);

            _sensor1.Name = "And Again";

            Assert.AreNotEqual(_sensor1.Name, _sensor1.RawName);
            Assert.AreEqual(rawName, _sensor1.RawName);
        }

        #endregion
    }
}
