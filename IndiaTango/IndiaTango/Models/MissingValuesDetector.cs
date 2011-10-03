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
    }
}
