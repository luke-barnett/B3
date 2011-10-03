using IndiaTango.Models;
using NUnit.Framework;

namespace IndiaTango.Tests
{
    [TestFixture]
    public class MissingValuesDetectorTests
    {
        MissingValuesDetector missingValuesDetector;

        [SetUp]
        public void SetUp()
        {
            missingValuesDetector = new MissingValuesDetector();
        }

        [Test]
        public void TestName()
        {
            Assert.AreEqual("Missing Values", missingValuesDetector.Name);
        }

        [Test]
        [RequiresSTA]
        public void SettingsGrid()
        {
            Assert.NotNull(missingValuesDetector.SettingsGrid);
        }
    }
}
