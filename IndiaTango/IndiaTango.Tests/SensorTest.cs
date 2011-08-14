using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IndiaTango.Models;
using NUnit.Framework;
using IndiaTango.Models;

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

        private Stack<SensorState> _testUndoStack;
        private Stack<SensorState> _secondUndoStack;
        private Stack<SensorState> _blankStack = new Stack<SensorState>();
        private List<DateTime> _blankCalibrationDates = new List<DateTime>();

        private Sensor _sensor1;
        private Sensor _sensor2;
        private Sensor _sensor3;
        private Sensor _undoSensor;
        private Sensor _secondUndoSensor;
        private Sensor _sensorEmpty;
        #endregion

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

            _sensor1 = new Sensor("Temperature", "Temperature at 10m", 100, 20, "°C", 0.003f, "Awesome Industries");
            _sensor2 = new Sensor("DO", "Dissolved Oxygen in the water", 50, 0, "%", 5.6f, "SensorPlus");

            // Initialise sensors for undo testing
            _undoSensor = new Sensor("Temperature", "C");
            _undoSensor.UndoStack = new Stack<SensorState>();
            _undoSensor.UndoStack.Push(new SensorState(new DateTime(2011, 8, 11),
                                                       new List<DataValue>(new DataValue[]
																	   {
																		   new DataValue(
																			   new DateTime(2011, 8, 12), 66.77f)
																	   })));
            _undoSensor.UndoStack.Push(new SensorState(new DateTime(2011, 8, 12),
                                            new List<DataValue>(new DataValue[]
															{
																new DataValue(
																	new DateTime(2011, 8, 12), 66.77f)
															})));

            _secondUndoSensor = new Sensor("Temperature", "C");
            _secondUndoSensor.UndoStack = new Stack<SensorState>();
            _secondUndoSensor.UndoStack.Push(new SensorState(new DateTime(2011, 3, 11),
                                                       new List<DataValue>(new DataValue[]
																   {
																	   new DataValue(
																		   new DateTime(2011, 8, 12), 66.77f)
																   })));
            _secondUndoSensor.UndoStack.Push(new SensorState(new DateTime(2011, 3, 12),
                                            new List<DataValue>(new DataValue[]
														{
															new DataValue(
																new DateTime(2011, 8, 12), 66.77f)
														})));

            _sensorEmpty = new Sensor("Temperature", "C");
            _sensorEmpty.UndoStack = new Stack<SensorState>();
        }

        #region Undo Stack Tests
        [Test]
        public void GetUndoStack()
        {
            var testSensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit, _testMaxRateOfChange, _testManufacturer, _testUndoStack, _blankStack, _blankCalibrationDates);

            Assert.AreEqual(_testUndoStack, testSensor.UndoStack);
        }

        [Test]
        public void SetUndoStack()
        {
            var testSensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit, _testMaxRateOfChange, _testManufacturer, _testUndoStack, _blankStack, _blankCalibrationDates);

            testSensor.UndoStack = _secondUndoStack;
            Assert.AreEqual(_secondUndoStack, testSensor.UndoStack);

            testSensor.UndoStack = _testUndoStack;
            Assert.AreEqual(_testUndoStack, testSensor.UndoStack);
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void SetUndoStackToNull()
        {
            var testSensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit, _testMaxRateOfChange, _testManufacturer, _testUndoStack, _blankStack, _blankCalibrationDates);
            testSensor.UndoStack = null;
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void SetRedoStackToNull()
        {
            var testSensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit, _testMaxRateOfChange, _testManufacturer, _testUndoStack, _blankStack, _blankCalibrationDates);
            testSensor.RedoStack = null;
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void SetCalibrationDatesListToNull()
        {
            var testSensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit, _testMaxRateOfChange, _testManufacturer, _testUndoStack, _blankStack, _blankCalibrationDates);
            testSensor.CalibrationDates = null;
        }
        #endregion

        #region Redo Stack Tests
        [Test]
        public void GetRedoStack()
        {
            var testSensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit, _testMaxRateOfChange, _testManufacturer, _testUndoStack, _secondUndoStack, _blankCalibrationDates);

            Assert.AreEqual(_secondUndoStack, testSensor.RedoStack);
        }

        [Test]
        public void SetRedoStack()
        {
            var testSensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit, _testMaxRateOfChange, _testManufacturer, _testUndoStack, _secondUndoStack, _blankCalibrationDates);
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
            var testSensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit, _testMaxRateOfChange, _testManufacturer, _testUndoStack, null, _blankCalibrationDates);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullUndoAndRedoStack()
        {
            var testSensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit, _testMaxRateOfChange, _testManufacturer, null, null, _blankCalibrationDates);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullUndoButNotRedoStack()
        {
            var testSensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit, _testMaxRateOfChange, _testManufacturer, null, _testUndoStack, _blankCalibrationDates);
        }

        [Test]
        public void MinimalConstructorsCreateEmptyStacksAndLists()
        {
            var testSensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit, _testMaxRateOfChange, _testManufacturer);

            Assert.IsNotNull(testSensor.UndoStack);
            Assert.IsNotNull(testSensor.RedoStack);
            Assert.IsNotNull(testSensor.CalibrationDates);
            Assert.IsTrue(testSensor.UndoStack.Count == 0 && testSensor.RedoStack.Count == 0 && testSensor.CalibrationDates.Count == 0);

            testSensor = new Sensor(_testName, _testUnit);

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

            Sensor testSensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit, _testMaxRateOfChange, _testManufacturer, _testUndoStack, _secondUndoStack, calibrationDatesTest);

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

            Sensor testSensor = new Sensor(_testName, _testDescription, _testUpperLimit, _testLowerLimit, _testUnit, _testMaxRateOfChange, _testManufacturer, _testUndoStack, _secondUndoStack, calibrationDatesTest);

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
        #endregion

        #region Invalid Values Tests
        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InvalidNameTest()
        {
            _sensor3 = new Sensor("", "", 0, 0, "%", 0, "");
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InvalidUnitTest()
        {
            _sensor3 = new Sensor("Temperature", "", 0, 0, "", 0, "");
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
        public void ValidUndoStackAfterUndo()
        {
            var correctStack = new Stack<SensorState>();
            correctStack.Push(new SensorState(new DateTime(2011, 8, 11),
                                           new List<DataValue>(new DataValue[]
																	   {
																		   new DataValue(
																			   new DateTime(2011, 8, 12), 66.77f)
																	   })));
            _undoSensor.Undo();
            Assert.AreEqual(correctStack, _undoSensor.UndoStack);

            var correctStackTwo = new Stack<SensorState>();
            correctStackTwo.Push(new SensorState(new DateTime(2011, 3, 11),
                                           new List<DataValue>(new DataValue[]
																   {
																	   new DataValue(
																		   new DateTime(2011, 8, 12), 66.77f)
																   })));
            _secondUndoSensor.Undo();
            Assert.AreEqual(correctStackTwo, _secondUndoSensor.UndoStack);

            var emptyStack = new Stack<SensorState>();
            _secondUndoSensor.Undo();
            Assert.AreEqual(emptyStack, _sensorEmpty.UndoStack);
        }

        [Test]
        public void ValidRedoStackAfterUndo()
        {
            var correctStack = new Stack<SensorState>();
            correctStack.Push(new SensorState(new DateTime(2011, 8, 12),
                                           new List<DataValue>(new DataValue[]
																	   {
																		   new DataValue(
																			   new DateTime(2011, 8, 12), 66.77f)
																	   })));

            _undoSensor.Undo();
            Assert.AreEqual(correctStack, _undoSensor.RedoStack);

            var correctStackTwo = new Stack<SensorState>();
            correctStackTwo.Push(new SensorState(new DateTime(2011, 3, 12),
                                           new List<DataValue>(new DataValue[]
																   {
																	   new DataValue(
																		   new DateTime(2011, 8, 12), 66.77f)
																   })));

            _secondUndoSensor.Undo();
            Assert.AreEqual(correctStackTwo, _secondUndoSensor.RedoStack);

            var emptyStack = new Stack<SensorState>();
            _secondUndoSensor.Undo();
            Assert.AreEqual(emptyStack, _sensorEmpty.RedoStack);
        }

        [Test]
        public void ValidUndoStackAfterRedo()
        {
            var redoSensor = new Sensor("Temperature", "C");
            redoSensor.UndoStack = new Stack<SensorState>();

            var dvA = new DataValue(new DateTime(2010, 5, 5, 22, 00, 0), 22.5f);
            var dvB = new DataValue(new DateTime(2010, 5, 5, 22, 15, 0), 21.4f);
            var dvC = new DataValue(new DateTime(2010, 5, 5, 22, 30, 0), 22.0f);

            var correctStack = new Stack<SensorState>(new SensorState[] { new SensorState(new DateTime(2011, 7, 5, 22, 47, 0), new List<DataValue>(new DataValue[] { dvA, dvB, dvC })) });

            redoSensor.RedoStack = new Stack<SensorState>(new SensorState[] { new SensorState(new DateTime(2011, 7, 5, 22, 47, 0), new List<DataValue>(new DataValue[] { dvA, dvB, dvC })) });
            redoSensor.Redo();

            Assert.AreEqual(correctStack, redoSensor.UndoStack);
        }

        [Test]
        public void ValidRedoStackAfterRedo()
        {
            var redoSensor = new Sensor("Temperature", "C");
            redoSensor.UndoStack = new Stack<SensorState>();

            var dvA = new DataValue(new DateTime(2010, 5, 5, 22, 00, 0), 22.5f);
            var dvB = new DataValue(new DateTime(2010, 5, 5, 22, 15, 0), 21.4f);
            var dvC = new DataValue(new DateTime(2010, 5, 5, 22, 30, 0), 22.0f);

            var correctStack = new Stack<SensorState>();

            redoSensor.RedoStack = new Stack<SensorState>(new SensorState[] { new SensorState(new DateTime(2011, 7, 5, 22, 47, 0), new List<DataValue>(new DataValue[] { dvA, dvB, dvC })) });
            redoSensor.Redo();

            Assert.AreEqual(correctStack, redoSensor.RedoStack);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage="Undo is not possible at this stage. There are no more possible states to undo to.")]
        public void ExceptionWhenUndoNotPossible()
        {
            _undoSensor.Undo();
            _undoSensor.Undo();
            _undoSensor.Undo(); // Should trigger the exception
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Redo is not possible at this stage. There are no more possible states to redo to.")]
        public void ExceptionWhenRedoNotPossible()
        {
            _undoSensor.Undo();
            _undoSensor.Undo();

            _undoSensor.Redo();
            _undoSensor.Redo();
            _undoSensor.Redo(); // Should trigger the exception
        }

        #endregion

        #region Multi Level Undo Tests
        [Test]
        public void ValidUndoStackAfterMultilevelUndo()
        {
            var emptyUndoStack = new Stack<SensorState>();

            // Undo twice to get empty stack in this case
            _undoSensor.Undo();
            _undoSensor.Undo();

            Assert.AreEqual(emptyUndoStack, _undoSensor.UndoStack);
        }

        [Test]
        public void ValidRedoStackAfterMultilevelUndo()
        {
            var correctRedoStack = new Stack<SensorState>();
            correctRedoStack.Push(new SensorState(new DateTime(2011, 8, 12),
                                                       new List<DataValue>(new DataValue[]
																	   {
																		   new DataValue(
																			   new DateTime(2011, 8, 12), 66.77f)
																	   })));
            correctRedoStack.Push(new SensorState(new DateTime(2011, 8, 11),
                                            new List<DataValue>(new DataValue[]
															{
																new DataValue(
																	new DateTime(2011, 8, 12), 66.77f)
															})));

            _undoSensor.Undo();
            _undoSensor.Undo();

            Assert.AreEqual(correctRedoStack, _undoSensor.RedoStack);
        }
        #endregion
    }
}
