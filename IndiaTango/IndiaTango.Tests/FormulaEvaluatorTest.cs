using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using NUnit.Framework;
using IndiaTango.Models;

namespace IndiaTango.Tests
{
	[TestFixture]
	class FormulaEvaluatorTest
	{
	    public FormulaEvaluator eval;

		[SetUp]
		public void Setup()
		{
		    eval = new FormulaEvaluator();
		}

		[Test]
		public void OnePlusOneTest()
		{
            Assert.AreEqual(2,eval.EvaluateFormula("1+1"));
		}

        [Test]
        public void ForLoopTest()
        {
            Assert.AreEqual(10, eval.EvaluateFormula("answer;\ndouble total = 0;\nfor(int i = 0; i < 5; i++)\ntotal+=i;\nanswer=total;"));
        }
	}
}
