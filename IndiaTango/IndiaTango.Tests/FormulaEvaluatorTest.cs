using System;
using System.CodeDom.Compiler;
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

        //Parsing not yet implemented

        //[Test]
        //public void ValidParseResults1()
        //{
        //    Assert.IsTrue(_eval.ParseFormula("x = x"));
        //}

        //[Test]
        //public void ValidParseResults2()
        //{
        //    Assert.IsTrue(_eval.ParseFormula("x = t.Day"));
        //}

        //[Test]
        //public void ValidParseResults3()
        //{
        //    Assert.IsTrue(_eval.ParseFormula("x = t.Month * 9"));
        //}

        //[Test]
        //public void InvalidParseResults1()
        //{
        //    Assert.IsFalse(_eval.ParseFormula("MessageBox.Show(\"hi\")"));
        //}

        //[Test]
        //public void InvalidParseResults2()
        //{
        //    Assert.IsFalse(_eval.ParseFormula("log"));
        //}

        //[Test]
        //public void InvalidParseResults3()
        //{
        //    Assert.IsFalse(_eval.ParseFormula("int i = 0"));
        //}

        [Test]
        public void ValidCompilerResults1()
        {
            CompilerResults results = _eval.CompileFormula("x = t.Month");
            Assert.IsTrue(_eval.CheckCompileResults(results));
        }

        [Test]
        public void ValidCompilerResults2()
        {
            CompilerResults results = _eval.CompileFormula("x = Cos(t.Month) * 7");
            Assert.IsTrue(_eval.CheckCompileResults(results));
        }

        [Test]
        public void ValidCompilerResults3()
        {
            CompilerResults results = _eval.CompileFormula("x = Pi * 8");
            Assert.IsTrue(_eval.CheckCompileResults(results));
        }

        [Test]
        public void InvalidCompilerResults1()
        {
            CompilerResults results = _eval.CompileFormula("x = potatoes");
            Assert.IsFalse(_eval.CheckCompileResults(results));
        }

        [Test]
        public void InvalidCompilerResults2()
        {
            CompilerResults results = _eval.CompileFormula("x = t.Potato");
            Assert.IsFalse(_eval.CheckCompileResults(results));
        }

        [Test]
        public void InvalidCompilerResults3()
        {
            CompilerResults results = _eval.CompileFormula("x = eleven");
            Assert.IsFalse(_eval.CheckCompileResults(results));
        }

		[Test]
		public void SetValuetoMonth()
		{
            CompilerResults results = _eval.CompileFormula("x = t.Month");
            SensorState newState = _eval.EvaluateFormula(results, _ds.Sensors[0].CurrentState.Clone(), _ds.StartTimeStamp,
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
            CompilerResults results = _eval.CompileFormula("x = Cos(t.Day)");
            SensorState newState = _eval.EvaluateFormula(results, _ds.Sensors[1].CurrentState.Clone(), _ds.StartTimeStamp,
                                                         _ds.EndTimeStamp);

            _ds.Sensors[1].AddState(newState);

            foreach (var pair in _ds.Sensors[1].CurrentState.Values)
            {
                Assert.AreEqual(Math.Cos(pair.Key.Day), pair.Value,delta);
            }
        }
	}
}
