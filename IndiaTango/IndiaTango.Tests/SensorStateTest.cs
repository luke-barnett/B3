using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using IndiaTango.Models;

namespace IndiaTango.Tests
{
    [TestFixture]
    class SensorStateTest
    {
        private DateTime testDate = new DateTime(2011, 08, 09, 12, 18, 54);
        private SensorState testSensorState;
        private DateTime modifiedDate = new DateTime(2011, 11, 17, 5, 0, 0);
        private List<DataValue> valueList = new List<DataValue>();
        private List<DataValue> secondValueList = new List<DataValue>();

        [SetUp]
        public void Setup()
        {
            testSensorState = new SensorState(testDate);

            valueList.AddRange(new DataValue[] { new DataValue(testDate, 55.2f), new DataValue(modifiedDate, 63.77f) });
            secondValueList.AddRange(new DataValue[] { new DataValue(new DateTime(2005, 11, 3, 14, 27, 12), 22.7f), new DataValue(new DateTime(2005, 11, 4, 14, 27, 28), 22.3f) });
        }

        #region Timestamp Tests
        [Test]
        public void GetEditTimestamp()
        {
            Assert.IsTrue(DateTime.Compare(testSensorState.EditTimestamp, testDate) == 0);

            SensorState modifiedSensorState = new SensorState(modifiedDate);
            Assert.IsTrue(DateTime.Compare(modifiedDate, modifiedSensorState.EditTimestamp) == 0);
        }

        [Test]
        public void SetEditTimestamp()
        {
            SensorState modifiedSensorState = new SensorState(testDate);
            modifiedSensorState.EditTimestamp = modifiedDate;

            Assert.IsTrue(DateTime.Compare(modifiedDate, modifiedSensorState.EditTimestamp) == 0);

            SensorState augustSensorState = new SensorState(modifiedDate);
            augustSensorState.EditTimestamp = testDate;
            Assert.IsTrue(DateTime.Compare(testDate, augustSensorState.EditTimestamp) == 0);
        }
        #endregion

        #region Value Tests
        [Test]
        public void GetValues()
        {
            SensorState valueSensorState = new SensorState(testDate, valueList);
            Assert.IsTrue(AllDataValuesCorrect(valueSensorState, valueList));

            SensorState secondValueSensorState = new SensorState(modifiedDate, secondValueList);
            Assert.IsTrue(AllDataValuesCorrect(secondValueSensorState, secondValueList));

            SensorState listCountWrongSensorState = new SensorState(testDate, valueList);
            List<DataValue> inconsistentValues = new List<DataValue>(new DataValue[] { new DataValue(testDate, 55.2f), new DataValue(modifiedDate, 63.77f), new DataValue(modifiedDate, 77.77f) });
            Assert.IsFalse(AllDataValuesCorrect(listCountWrongSensorState, inconsistentValues));
        }

        [Test]
        public void SetValues()
        {
            SensorState valueSensorState = new SensorState(testDate, valueList);
            valueSensorState.Values = valueList;
            Assert.IsTrue(AllDataValuesCorrect(valueSensorState, valueList));
            
            SensorState secondValueSensorState = new SensorState(modifiedDate, secondValueList);
            secondValueSensorState.Values = secondValueList;
            Assert.IsTrue(AllDataValuesCorrect(secondValueSensorState, secondValueList));
        }
        #endregion

        #region Constructor Argument Tests
        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullValueList()
        {
            SensorState testState = new SensorState(DateTime.Now, null);
        }

        [Test]
        public void AllowConstructionWithOnlyEditTimestamp()
        {
            SensorState testState = new SensorState(DateTime.Now);
            Assert.Pass();
        }

        [Test]
        public void ConstructionWithOnlyEditTimestampCreatesNewList()
        {
            SensorState testState = new SensorState(DateTime.Now);

            Assert.NotNull(testState.Values);
            Assert.IsTrue(testState.Values.Count == 0);
        }
        #endregion

        #region Test Convenience Methods
        private bool AllDataValuesCorrect(SensorState sensorState, List<DataValue> listOfValues)
        {
            if (sensorState.Values.Count != listOfValues.Count)
                return false;

            for (int i = 0; i < listOfValues.Count; i++)
                if (!sensorState.Values[i].Equals(listOfValues[i]))
                    return false;

            return true;
        }
        #endregion



        [Test]
        public void FindMissingValuesTest()
        {
            var missingDates = new List<DataValue>
                                   {
                                       new DataValue(new DateTime(2011, 8, 20, 0, 15, 0), 0),
                                       new DataValue(new DateTime(2011, 8, 20, 0, 30, 0), 0),
                                       new DataValue(new DateTime(2011, 8, 20, 0, 45, 0), 0),
                                       new DataValue(new DateTime(2011, 8, 20, 1, 0, 0), 0),
                                       new DataValue(new DateTime(2011, 8, 20, 1, 15, 0), 0),
                                       new DataValue(new DateTime(2011, 8, 20, 1, 30, 0), 0),
                                       new DataValue(new DateTime(2011, 8, 20, 1, 45, 0), 0)
                                   };
            var sensorState = new SensorState(new DateTime(2011, 8, 23, 0, 0, 0));
            sensorState.Values = new List<DataValue>
                                     {
                                         new DataValue(new DateTime(2011, 8, 20, 0, 0, 0), 100),
                                         new DataValue(new DateTime(2011, 8, 20, 2, 0, 0), 50)
                                     };
            Assert.AreEqual(missingDates, sensorState.GetMissingTimes(15));
        }
    }
}
