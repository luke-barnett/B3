using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using IndiaTango.Models;

namespace IndiaTango.Tests
{
	[TestFixture]
	public class ToolsTest
	{
		[Test]
		public void MD5HasherTest()
		{
			Assert.AreEqual("e7b91a4f4b504c924c354a8f9e6bc54e", Tools.GenerateMD5HashFromFile(Path.Combine(Common.TestDataPath, "data.txt")));
            Assert.AreEqual("fe88e7c801c79e414732d540e7460205", Tools.GenerateMD5HashFromFile(Path.Combine(Common.TestDataPath, "pocket.txt")));
		}
	}
}
