using System;
using NUnit.Framework;
using IndiaTango.Models;

namespace IndiaTango.Tests
{
    [TestFixture]
    class ErroneousValueTests
    {
        private ErroneousValue value;
        private ErroneousValue valueWithDetector;
        private MissingValuesDetector detector;

        [SetUp]
        public void SetUp()
        {
            detector = new MissingValuesDetector();

            value = new ErroneousValue(DateTime.Now, 15);

            valueWithDetector = new ErroneousValue(DateTime.Now, 50, detector);
        }
    }
}
