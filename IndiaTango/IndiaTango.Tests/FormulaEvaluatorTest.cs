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
        private Site _s = new Site(1, "dsf", "asdf", new Contact("asdf", "asdf", "adsf@sdfg.com", "uerh", "sadf"), new Contact("asdf", "asdf", "adsf@sdfg.com", "uerh", "sadf"), new GPSCoords(32, 5));
        private double delta = 0.0000001;

        [SetUp]
        public void Setup()
        {
            _reader = new CSVReader(Path.Combine(Common.TestDataPath, "lakeTutira120120110648_extra_small.csv"));
            _ds = new Dataset(_s, _reader.ReadSensors());
            var sensorVariables = SensorVariable.CreateSensorVariablesFromSensors(_ds.Sensors);
            foreach (var sensor in _ds.Sensors)
            {
                sensor.Owner = _ds;
                sensor.Variable = sensorVariables.FirstOrDefault(x => x.Sensor == sensor);
            }

            _eval = new FormulaEvaluator(_ds.Sensors);
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
            Formula formula = _eval.CompileFormula("a = time.Month");
            Assert.IsTrue(formula.IsValid);
        }

        [Test]
        public void ValidCompilerResults2()
        {
            Formula formula = _eval.CompileFormula("a = Cos(time.Month) * 7");
            Assert.IsTrue(formula.IsValid);
        }

        [Test]
        public void ValidCompilerResults3()
        {
            Formula formula = _eval.CompileFormula("a = Pi * 8");
            Assert.IsTrue(formula.IsValid);
        }

        [Test]
        public void InvalidCompilerResults1()
        {
            Formula formula = _eval.CompileFormula("a = potatoes");
            Assert.IsTrue(!formula.IsValid);
        }

        [Test]
        public void InvalidCompilerResults2()
        {
            Formula formula = _eval.CompileFormula("a = time.Potato");
            Assert.IsTrue(!formula.IsValid);
        }

        [Test]
        public void InvalidCompilerResults3()
        {
            Formula formula = _eval.CompileFormula("a = eleven");
            Assert.IsTrue(!formula.IsValid);
        }

        [Test]
        public void SetValuetoMonth()
        {
            Formula formula = _eval.CompileFormula("a = time.Month");
            var result = _eval.EvaluateFormula(formula, _ds.StartTimeStamp, _ds.EndTimeStamp, false, new ChangeReason(0, "Test"));

            foreach (var pair in result.Value.Values)
            {
                Assert.AreEqual(pair.Key.Month, pair.Value);
            }
        }

        [Test]
        public void SetValuetoCosDay()
        {
            Formula formula = _eval.CompileFormula("b = Cos(time.Day)");
            var result = _eval.EvaluateFormula(formula, _ds.StartTimeStamp, _ds.EndTimeStamp, false, new ChangeReason(0, "Test"));

            foreach (var pair in result.Value.Values)
            {
                Assert.AreEqual(Math.Cos(pair.Key.Day), pair.Value, delta);
            }
        }

        [Test]
        public void SetLToM()
        {
            Formula formula = _eval.CompileFormula("l = m ");
            var result = _eval.EvaluateFormula(formula, _ds.StartTimeStamp, _ds.EndTimeStamp, false, new ChangeReason(0, "Test"));

            foreach (var pair in _ds.Sensors[12].CurrentState.Values)
            {
                Assert.AreEqual(pair.Value, result.Value.Values[pair.Key]);
            }
        }

        [Test]
        public void SensorVariableTest()
        {
            string expected = "a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z,aa";
            string actual = "";
            List<SensorVariable> variables = SensorVariable.CreateSensorVariablesFromSensors(_ds.Sensors);
            foreach (SensorVariable sensorVariable in variables)
                actual += sensorVariable.VariableName + ",";

            actual = actual.Remove(actual.Length - 1);
            Assert.AreEqual(expected, actual);
        }
    }
}
