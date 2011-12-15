using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using IndiaTango.Models;
using System.IO;

namespace IndiaTango.Tests
{
    [TestFixture]
    class SensorStateTest
    {
        private DateTime testDate = new DateTime(2011, 08, 09, 12, 18, 54);
        private SensorState testSensorState;
        private DateTime modifiedDate = new DateTime(2011, 11, 17, 5, 0, 0);
        private Dictionary<DateTime, float> valueList;
        private Dictionary<DateTime, float> secondValueList;
        private ChangeReason _reason = new ChangeReason(0, "Out of range");
        private DateTime baseDate = new DateTime(2011, 5, 7, 12, 15, 0);
        private Contact _sampleContact = new Contact("Steven", "McTainsh", "steven@mctainsh.com", "Awesome", "1212121");
        private Sensor _testSensor;

        [SetUp]
        public void Setup()
        {
            _testSensor = new Sensor("Temperature1", "Temp at 100m", 100, 0, "C", 2, null);
            testSensorState = new SensorState(_testSensor, testDate);
            valueList = new Dictionary<DateTime, float>();
            secondValueList = new Dictionary<DateTime, float>();
            valueList.Add(testDate, 55.2f);
            valueList.Add(modifiedDate, 63.77f);
            secondValueList.Add(new DateTime(2005, 11, 3, 14, 27, 12), 22.7f);
            secondValueList.Add(new DateTime(2005, 12, 4, 14, 27, 28), 22.3f);
        }

        #region Timestamp Tests
        [Test]
        public void GetEditTimestamp()
        {
            Assert.IsTrue(DateTime.Compare(testSensorState.EditTimestamp, testDate) == 0);

            SensorState modifiedSensorState = new SensorState(_testSensor, modifiedDate);
            Assert.IsTrue(DateTime.Compare(modifiedDate, modifiedSensorState.EditTimestamp) == 0);
        }

        [Test]
        public void SetEditTimestamp()
        {
            SensorState modifiedSensorState = new SensorState(_testSensor, testDate);
            modifiedSensorState.EditTimestamp = modifiedDate;

            Assert.IsTrue(DateTime.Compare(modifiedDate, modifiedSensorState.EditTimestamp) == 0);

            SensorState augustSensorState = new SensorState(_testSensor, modifiedDate);
            augustSensorState.EditTimestamp = testDate;
            Assert.IsTrue(DateTime.Compare(testDate, augustSensorState.EditTimestamp) == 0);
        }
        #endregion

        #region Value Tests
        [Test]
        public void GetValues()
        {
            SensorState valueSensorState = new SensorState(_testSensor, testDate, valueList, null);
            Assert.IsTrue(AllDataValuesCorrect(valueSensorState, valueList));

            SensorState secondValueSensorState = new SensorState(_testSensor, modifiedDate, secondValueList, null);
            Assert.IsTrue(AllDataValuesCorrect(secondValueSensorState, secondValueList));

            SensorState listCountWrongSensorState = new SensorState(_testSensor, testDate, valueList, null);
            var inconsistentValues = new Dictionary<DateTime, float> { { testDate, 55.2f }, { modifiedDate, 63.77f }, { modifiedDate.AddDays(1), 77.77f } };
            Assert.IsFalse(AllDataValuesCorrect(listCountWrongSensorState, inconsistentValues));
        }

        [Test]
        public void SetValues()
        {
            SensorState valueSensorState = new SensorState(_testSensor, testDate, valueList, null);
            valueSensorState.Values = valueList;
            Assert.IsTrue(AllDataValuesCorrect(valueSensorState, valueList));

            SensorState secondValueSensorState = new SensorState(_testSensor, modifiedDate, secondValueList, null);
            secondValueSensorState.Values = secondValueList;
            Assert.IsTrue(AllDataValuesCorrect(secondValueSensorState, secondValueList));
        }
        #endregion

        #region Constructor Argument Tests
        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullValueList()
        {
            var testState = new SensorState(_testSensor, DateTime.Now, null, null);
        }

        [Test]
        public void AllowConstructionWithOnlyEditTimestamp()
        {
            var testState = new SensorState(_testSensor, DateTime.Now);
            Assert.Pass();
        }

        [Test]
        public void SetsRawCorrectlyOnConstruction()
        {
            var testState = new SensorState(_testSensor, DateTime.Now, new Dictionary<DateTime, float>(), null, true, null);
            Assert.IsTrue(testState.IsRaw);

            testState = new SensorState(_testSensor, DateTime.Now, new Dictionary<DateTime, float>(), null, false, null);
            Assert.IsTrue(!testState.IsRaw);
        }

        [Test]
        public void ConstructionWithOnlyEditTimestampCreatesNewList()
        {
            var testState = new SensorState(_testSensor, DateTime.Now.AddDays(20));

            Assert.NotNull(testState.Values);
            Assert.IsTrue(testState.Values.Count == 0);
        }
        #endregion

        #region Test Convenience Methods
        private bool AllDataValuesCorrect(SensorState sensorState, Dictionary<DateTime, float> listOfValues)
        {
            if (sensorState.Values.Count != listOfValues.Count)
                return false;

            //for (int i = 0; i < listOfValues.Count; i++)
            //    if (!sensorState.Values[i].Equals(listOfValues[i]))
            //        return false;
            foreach (var key in listOfValues.Keys)
            {
                if (!listOfValues.Keys.Contains(key))
                    return false;
                if (!sensorState.Values[key].Equals(listOfValues[key]))
                    return false;
            }

            return true;
        }
        #endregion

        [Test]
        public void FindMissingValuesTest()
        {
            var missingDates = new List<DateTime>
                                   {
                                       new DateTime(2011, 8, 20, 0, 15, 0),
                                       new DateTime(2011, 8, 20, 0, 30, 0),
                                       new DateTime(2011, 8, 20, 0, 45, 0),
                                       new DateTime(2011, 8, 20, 1, 0, 0),
                                       new DateTime(2011, 8, 20, 1, 15, 0),
                                       new DateTime(2011, 8, 20, 1, 30, 0),
                                       new DateTime(2011, 8, 20, 1, 45, 0)
                                   };
            var sensorState = new SensorState(_testSensor, new DateTime(2011, 8, 23, 0, 0, 0));
            sensorState.Values = new Dictionary<DateTime, float>
                                     {
                                         {new DateTime(2011, 8, 20, 0, 0, 0), 100},
                                         {new DateTime(2011, 8, 20, 2, 0, 0), 50}
                                     };
            Assert.AreEqual(missingDates, sensorState.GetMissingTimes(15, new DateTime(2011, 8, 20, 0, 0, 0), new DateTime(2011, 8, 20, 2, 0, 0)));
        }

        [Test]
        public void GetsChangeReasonCorrectly()
        {

            var s = new SensorState(_testSensor, DateTime.Now,
                                    new Dictionary<DateTime, float> { { new DateTime(2011, 5, 7, 12, 20, 0), 200 } }, _reason, true, null);
            Assert.AreEqual(_reason, s.Reason);
        }

        [Test]
        public void SetsChangeReasonCorrectly()
        {
            var s = new SensorState(_testSensor, DateTime.Now,
                                    new Dictionary<DateTime, float> { { new DateTime(2011, 5, 7, 12, 20, 0), 200 } }, null);
            Assert.AreEqual(null, s.Reason);

            s.Reason = _reason;
            Assert.AreEqual(_reason, s.Reason);
        }

        [Test]
        public void EqualityTest()
        {
            var a = new SensorState(_testSensor, baseDate,
                                    new Dictionary<DateTime, float> { { baseDate.AddMinutes(15), 200 }, { baseDate.AddMinutes(30), 200 }, { baseDate.AddMinutes(45), 200 }, { baseDate.AddMinutes(60), 200 } }, null);
            var b = new SensorState(_testSensor, baseDate,
                                    new Dictionary<DateTime, float> { { baseDate.AddMinutes(15), 200 }, { baseDate.AddMinutes(30), 200 }, { baseDate.AddMinutes(45), 200 }, { baseDate.AddMinutes(60), 200 } }, null);
            var c = new SensorState(_testSensor, baseDate,
                                    new Dictionary<DateTime, float> { { baseDate.AddMinutes(15), 200 }, { baseDate.AddMinutes(30), 10 }, { baseDate.AddMinutes(45), 200 }, { baseDate.AddMinutes(60), 50 } }, null);
            var d = new SensorState(_testSensor, baseDate,
                                    new Dictionary<DateTime, float> { { baseDate.AddMinutes(15), 200 }, { baseDate.AddMinutes(30), 200 }, { baseDate.AddMinutes(45), 200 }, { baseDate.AddMinutes(60), 200 } }, null);
            var e = new SensorState(_testSensor, baseDate.AddHours(50),
                                    new Dictionary<DateTime, float> { { baseDate.AddMinutes(15), 200 }, { baseDate.AddMinutes(30), 200 }, { baseDate.AddMinutes(45), 200 }, { baseDate.AddMinutes(60), 200 } }, null);
            var f = new SensorState(_testSensor, baseDate,
                                    new Dictionary<DateTime, float> { { baseDate.AddMinutes(45), 200 }, { baseDate.AddMinutes(60), 200 }, { baseDate.AddMinutes(75), 200 }, { baseDate.AddMinutes(90), 200 } }, null);

            Assert.AreEqual(a, b);
            Assert.AreNotEqual(b, c);
            Assert.AreNotEqual(c, d);
            Assert.AreNotEqual(d, e);
            Assert.AreNotEqual(e, f);
        }

        #region Extrapolation Test
        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void InterpolateWithNoKeys()
        {
            var ds =
                new Dataset(new Site(10, "Lake Rotorua", "Steven McTainsh", _sampleContact, _sampleContact,
                                     new GPSCoords(50, 50)), new List<Sensor> { { _testSensor } });

            var a = new SensorState(_testSensor, baseDate,
                                       new Dictionary<DateTime, float> { { baseDate.AddMinutes(15), 200 }, { baseDate.AddMinutes(30), 200 }, { baseDate.AddMinutes(45), 200 }, { baseDate.AddMinutes(60), 200 } }, null);
            a.Interpolate(new List<DateTime>(), ds, new ChangeReason(0, "Test"));

        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InterpolateWithNullKeys()
        {
            var ds =
                new Dataset(new Site(10, "Lake Rotorua", "Steven McTainsh", _sampleContact, _sampleContact,
                                     new GPSCoords(50, 50)), new List<Sensor> { _testSensor });

            var a = new SensorState(_testSensor, baseDate,
                                       new Dictionary<DateTime, float> { { baseDate.AddMinutes(15), 200 }, { baseDate.AddMinutes(30), 200 }, { baseDate.AddMinutes(45), 200 }, { baseDate.AddMinutes(60), 200 } }, null);
            a.Interpolate(null, ds, new ChangeReason(0, "Test"));

        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InterpolateWithNullDataset()
        {
            var ds =
                new Dataset(new Site(10, "Lake Rotorua", "Steven McTainsh", _sampleContact, _sampleContact,
                                     new GPSCoords(50, 50)), new List<Sensor> { { _testSensor } });

            var A = new SensorState(_testSensor, baseDate,
                                       new Dictionary<DateTime, float> { { baseDate.AddMinutes(15), 200 }, { baseDate.AddMinutes(30), 200 }, { baseDate.AddMinutes(45), 200 }, { baseDate.AddMinutes(60), 200 } }, null);
            A.Interpolate(new List<DateTime> { { baseDate.AddMinutes(60) } }, null, new ChangeReason(0, "Test"));

        }

        [Test]
        public void InterpolatesCorrectlyOneMissingPt()
        {
            var ds =
                new Dataset(new Site(10, "Lake Rotorua", "Steven McTainsh", _sampleContact, _sampleContact,
                                     new GPSCoords(50, 50)), new List<Sensor> { _testSensor });
            ds.DataInterval = 15;

            var a = new SensorState(_testSensor, baseDate,
                                       new Dictionary<DateTime, float> { { baseDate.AddMinutes(15), 50 }, { baseDate.AddMinutes(30), 100 }, { baseDate.AddMinutes(60), 200 } }, null);

            var state = a.Interpolate(new List<DateTime> { baseDate.AddMinutes(45) }, ds, new ChangeReason(0, "Test"));

            Assert.AreEqual(150, state.Values[baseDate.AddMinutes(45)]);
        }

        [Test]
        public void InterpolatesCorrectlyTwoMissingPts()
        {
            var ds =
                new Dataset(new Site(10, "Lake Rotorua", "Steven McTainsh", _sampleContact, _sampleContact,
                                     new GPSCoords(50, 50)), new List<Sensor> { { _testSensor } });
            ds.DataInterval = 15;

            var A = new SensorState(_testSensor, baseDate,
                                       new Dictionary<DateTime, float> { { baseDate.AddMinutes(15), 50 }, { baseDate.AddMinutes(60), 200 } }, null);

            var state = A.Interpolate(new List<DateTime> { baseDate.AddMinutes(30), baseDate.AddMinutes(45) }, ds, new ChangeReason(0, "Test"));

            Assert.AreEqual(100, state.Values[baseDate.AddMinutes(30)]);
            Assert.AreEqual(150, state.Values[baseDate.AddMinutes(45)]);
        }
        #endregion

        #region Make Values Zero Test
        [Test]
        public void SingleValueMadeZero()
        {
            var date = new DateTime(2011, 7, 7, 12, 15, 0);

            var list = new List<DateTime>();
            list.Add(date);

            var oldState = new SensorState(_testSensor, DateTime.Now, new Dictionary<DateTime, float>(), null);
            var newState = oldState.MakeZero(list, new ChangeReason(0, "Test"));

            Assert.AreEqual(0, newState.Values[date]);
        }

        [Test]
        public void MultipleValuesMadeZero()
        {
            var date = new DateTime(2011, 7, 7, 12, 15, 0);

            var list = new List<DateTime>();
            list.Add(date.AddMinutes(15));
            list.Add(date.AddMinutes(30));
            list.Add(date.AddMinutes(45));

            var oldState = new SensorState(_testSensor, DateTime.Now, new Dictionary<DateTime, float>(), null);
            oldState.Values.Add(date, 5000);

            var newState = oldState.MakeZero(list, new ChangeReason(0, "Test"));

            Assert.AreNotEqual(0, newState.Values[date]);
            Assert.AreEqual(0, newState.Values[list[0]]);
            Assert.AreEqual(0, newState.Values[list[1]]);
            Assert.AreEqual(0, newState.Values[list[2]]);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullZeroValueList()
        {
            var oldState = new SensorState(_testSensor, DateTime.Now, new Dictionary<DateTime, float>(), null);
            oldState.MakeZero(null, new ChangeReason(0, "Test"));
        }

        [Test]
        public void SingleValueMadeDifferentValue()
        {
            // TODO: could move these to SetUp
            var date = new DateTime(2011, 7, 7, 12, 15, 0);

            var list = new List<DateTime>();
            list.Add(date);

            var oldState = new SensorState(_testSensor, DateTime.Now, new Dictionary<DateTime, float>(), null);
            var newState = oldState.MakeValue(list, 5, new ChangeReason(0, "Test"));

            Assert.AreEqual(5, newState.Values[date]);
        }

        [Test]
        public void MultipleValuesMadeDifferentValue()
        {
            var date = new DateTime(2011, 7, 7, 12, 15, 0);

            var list = new List<DateTime>();
            list.Add(date.AddMinutes(15));
            list.Add(date.AddMinutes(30));
            list.Add(date.AddMinutes(45));

            var oldState = new SensorState(_testSensor, DateTime.Now, new Dictionary<DateTime, float>(), null);
            oldState.Values.Add(date, 5000);

            var newState = oldState.MakeValue(list, 20, new ChangeReason(0, "Test"));

            Assert.AreNotEqual(20, newState.Values[date]);
            Assert.AreEqual(20, newState.Values[list[0]]);
            Assert.AreEqual(20, newState.Values[list[1]]);
            Assert.AreEqual(20, newState.Values[list[2]]);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullMakeValueList()
        {
            var oldState = new SensorState(_testSensor, DateTime.Now, new Dictionary<DateTime, float>(), null);
            oldState.MakeValue(null, 5, new ChangeReason(0, "Test"));
        }
        #endregion

        #region Outlier detection tests

        [Test]
        public void MinMaxMethod()
        {
            var date1 = new DateTime(2011, 1, 1, 0, 0, 0);
            var date2 = new DateTime(2011, 1, 1, 0, 15, 0);
            testSensorState.Values.Add(date1, 3f);
            testSensorState.Values.Add(date2, 1.5f);
            var stuff = testSensorState.GetOutliersFromMaxAndMin(15, date1, date2, 1, 2,
                                                           100);
            Assert.Contains(date1, stuff);
        }

        [Test]
        public void RateOfChangeMethod()
        {
            var date1 = new DateTime(2011, 1, 1, 0, 0, 0);
            var date2 = new DateTime(2011, 1, 1, 0, 15, 0);
            testSensorState.Values.Add(date1, 0f);
            testSensorState.Values.Add(date2, 5f);
            var stuff = testSensorState.GetOutliersFromMaxAndMin(15, date1, date2, 1, 10,
                                                           4);
            Assert.Contains(date2, stuff);
        }

        [Test]
        public void StdDevMethod()
        {
            var date1 = new DateTime(2011, 1, 1, 0, 0, 0);
            var date2 = new DateTime(2011, 1, 1, 0, 15, 0);
            var date3 = new DateTime(2011, 1, 1, 0, 30, 0);
            var date4 = new DateTime(2011, 1, 1, 0, 45, 0);
            var date5 = new DateTime(2011, 1, 1, 1, 0, 0);
            var date6 = new DateTime(2011, 1, 1, 1, 15, 0);
            testSensorState.Values.Add(date1, 1f);
            testSensorState.Values.Add(date2, 7f);
            testSensorState.Values.Add(date3, 3f);
            testSensorState.Values.Add(date4, 15f);
            var outliers = testSensorState.GetOutliersFromStdDev(15, date1, date4, 1, 4);
            Assert.IsEmpty(outliers);
            testSensorState.Values.Add(date5, 11f);
            outliers = testSensorState.GetOutliersFromStdDev(15, date1, date5, 1, 4);
            Assert.Contains(date3, outliers);
            testSensorState.Values.Add(date6, 2);
            outliers = testSensorState.GetOutliersFromStdDev(15, date1, date6, 1, 4);
            Assert.Contains(date4, outliers);

        }

        #endregion
    }
}
