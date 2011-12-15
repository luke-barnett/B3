using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using IndiaTango.Models;

namespace IndiaTango.Tests
{
    [TestFixture]
    class CalibrationTest
    {
        private CSVReader _reader;
        private Site _s = new Site(1, "dsf", "asdf", new Contact("asdf", "asdf", "adsf@sdfg.com", "uerh", "sadf"), new Contact("asdf", "asdf", "adsf@sdfg.com", "uerh", "sadf"), new GPSCoords(32, 5));
        private Dataset _ds;
        private double delta = 0.0001;

        [SetUp]
        public void SetUp()
        {
            _reader = new CSVReader(Path.Combine(Common.TestDataPath, "calibrationTestData.csv"));
            _ds = new Dataset(_s, _reader.ReadSensors());
        }

        [Test]
        public void StraightLineTest()
        {
            _ds.Sensors[0].AddState(_ds.Sensors[0].CurrentState.Calibrate(_ds.StartTimeStamp, _ds.EndTimeStamp, 20, 15, 119.6f, 114.6f, new ChangeReason(0, "Test")));

            //Need a better way of doing this perhaps?
            foreach (var pair in _ds.Sensors[0].CurrentState.Values)
            {
                //Console.WriteLine(pair.Value + " " + _ds.Sensors[1].CurrentState.Values[pair.Key]);
                Assert.AreEqual(_ds.Sensors[1].CurrentState.Values[pair.Key], pair.Value, delta);
            }

            //Assert.IsTrue(_ds.Sensors[0].CurrentState.Values.Count == _ds.Sensors[1].CurrentState.Values.Count 
            //	&& !_ds.Sensors[0].CurrentState.Values.Except(_ds.Sensors[1].CurrentState.Values).Any());
            //Assert.IsTrue(_ds.Sensors[0].CurrentState.Values.SequenceEqual(_ds.Sensors[1].CurrentState.Values));
            //Assert.IsTrue(_ds.Sensors[0].CurrentState.Values.Values.Equals(_ds.Sensors[1].CurrentState.Values.Values));

        }

        [Test]
        public void StraightFluxLineTest()
        {
            _ds.Sensors[2].AddState(_ds.Sensors[2].CurrentState.Calibrate(_ds.StartTimeStamp, new DateTime(2009, 1, 9, 16, 0, 0), 20, 15, 30f, 5f, new ChangeReason(0, "Test")));

            foreach (var pair in _ds.Sensors[2].CurrentState.Values)
            {
                Assert.AreEqual(_ds.Sensors[3].CurrentState.Values[pair.Key], pair.Value, delta);
            }
        }
    }
}
