using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using NUnit.Framework;
using IndiaTango.Models;

namespace IndiaTango.Tests
{
	[TestFixture]
	class FormulaEvaluatorTest
	{
	    private FormulaEvaluator _eval;
	    private Dataset _ds;
	    private CSVReader _reader;
        private Site _s = new Site(1, "dsf", "asdf", new Contact("asdf", "asdf", "adsf@sdfg.com", "uerh", "sadf"), new Contact("asdf", "asdf", "adsf@sdfg.com", "uerh", "sadf"), null, new GPSCoords(32, 5));
	    private double delta = 0.0000001;

		[SetUp]
		public void Setup()
		{
            _reader = new CSVReader(Path.Combine(Common.TestDataPath, "lakeTutira120120110648_extra_small.csv"));
            _ds = new Dataset(_s, _reader.ReadSensors());

            _eval = new FormulaEvaluator();
		}

		[Test]
		public void SetValuetoMonth()
		{
		    SensorState newState = _eval.EvaluateFormula("x = t.Month", _ds.Sensors[0].CurrentState.Clone(), _ds.StartTimeStamp,
		                                                 _ds.EndTimeStamp);

            _ds.Sensors[0].AddState(newState);

            foreach (var pair in _ds.Sensors[0].CurrentState.Values)
		    {
		        Assert.AreEqual(pair.Key.Month,pair.Value);
		    }
		}

        [Test]
        public void SetValuetoCosDay()
        {
            SensorState newState = _eval.EvaluateFormula("x = Cos(t.Day)", _ds.Sensors[1].CurrentState.Clone(), _ds.StartTimeStamp,
                                                         _ds.EndTimeStamp);

            _ds.Sensors[1].AddState(newState);

            foreach (var pair in _ds.Sensors[1].CurrentState.Values)
            {
                Assert.AreEqual(Math.Cos(pair.Key.Day), pair.Value,delta);
            }
        }
	}
}
