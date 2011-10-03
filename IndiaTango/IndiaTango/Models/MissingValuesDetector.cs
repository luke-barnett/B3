using System.Collections.Generic;

namespace IndiaTango.Models
{
    public class MissingValuesDetector : IDetectionMethod
    {
        public override string ToString()
        {
            return "Missing Values";
        }

        public IDetectionMethod This
        {
            get { return this; }
        }

        public List<string> GetDetectedValues()
        {
            return new List<string> {"This is a bad bad value"};
        }
    }
}
