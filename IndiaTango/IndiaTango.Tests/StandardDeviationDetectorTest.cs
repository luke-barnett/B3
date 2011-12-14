using System;
using NUnit.Framework;
using IndiaTango.Models;

namespace IndiaTango.Tests
{
    [TestFixture]
    public class StandardDeviationDetectorTest
    {
        private RunningMeanStandardDeviationDetector _detector;
        private Sensor _sensor;
        private ErroneousValue _erroneousValue;

        [SetUp]
        public void SetUp()
        {
            _detector = new RunningMeanStandardDeviationDetector { SmoothingPeriod = 240, NumberOfStandardDeviations = 0.5f };
            _sensor = new Sensor("Test Sensor", "Used in testing", 100, 10, "Unit", 5, new Dataset(null) { DataInterval = 60 });
            _sensor.AddState(new SensorState(_sensor, DateTime.Now));
            _sensor.CurrentState.Values.Add(DateTime.Now.AddHours(1), 5);
            _sensor.CurrentState.Values.Add(DateTime.Now.AddHours(2), 5);
            _sensor.CurrentState.Values.Add(DateTime.Now.AddHours(3), 5);
            _erroneousValue = new ErroneousValue(DateTime.Now.AddHours(4), 600, _sensor);
            _sensor.CurrentState.Values.Add(_erroneousValue.TimeStamp, _erroneousValue.Value);
            _sensor.CurrentState.Values.Add(DateTime.Now.AddHours(5), 5);
            _sensor.CurrentState.Values.Add(DateTime.Now.AddHours(6), 5);
            _sensor.CurrentState.Values.Add(DateTime.Now.AddHours(7), 5);
        }

        [Test]
        public void CheckName()
        {
            Assert.AreEqual("Running Mean with Standard Deviation", _detector.Name);
        }

        [Test]
        public void ReturnsItself()
        {
            Assert.AreEqual(_detector, _detector.This);
        }

        [Test]
        public void CheckGraphableSeriesBooleanWhenNothingCalculated()
        {
            _detector.ShowGraph = true;
            Assert.False(_detector.HasGraphableSeries);
        }

        [Test]
        public void CheckForErroneousValues()
        {
            Assert.Contains(_erroneousValue, _detector.GetDetectedValues(_sensor));
        }

        [Test]
        public void CheckGraphableSeriesBooleanWithSomethingCalculated()
        {
            CheckGraphableSeriesBooleanWhenNothingCalculated();
            CheckForErroneousValues();
            Assert.True(_detector.HasGraphableSeries);
        }

        [Test]
        public void CheckThatItDoesHaveSettings()
        {
            Assert.True(_detector.HasSettings);
        }

        [Test]
        [RequiresSTA]
        public void CheckThatWeCanGetTheSettingsGrid()
        {
            Assert.IsNotNull(_detector.SettingsGrid);
        }

        [Test]
        public void CheckThatItHasNoChildren()
        {
            Assert.IsEmpty(_detector.Children);
        }

        [Test]
        public void CheckDetectsIndividualValueCorrectly()
        {
            Assert.False(_detector.CheckIndividualValue(_sensor, _erroneousValue.TimeStamp));
        }

        [Test]
        public void CheckGraphsAreEmptyIfNothingCalculated()
        {
            Assert.IsEmpty(_detector.GraphableSeries(_sensor, DateTime.Now, DateTime.Now.AddDays(1)));
        }

        [Test]
        [RequiresSTA]
        public void CheckGraphsAreNotEmptyWhenThingsAreCalculated()
        {
            CheckGraphsAreEmptyIfNothingCalculated();
            CheckDetectsIndividualValueCorrectly();
            _detector.ShowGraph = true;
            Assert.IsNotEmpty(_detector.GraphableSeries(_sensor, DateTime.Now, DateTime.Now.AddDays(1)));
        }
    }
}