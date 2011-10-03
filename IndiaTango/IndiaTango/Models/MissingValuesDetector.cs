using System.Collections.Generic;
using System.Windows.Controls;

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
            return new List<string> { "This is a bad bad value" };
        }

        public bool HasSettings
        {
            get { return false; }
        }

        public Grid SettingsGrid
        {
            get
            {
                var wrapperGrid = new Grid();
                wrapperGrid.Children.Add(new TextBlock { Text = "No Settings" });
                return wrapperGrid;
            }
        }

        public bool HasGraphableSeries
        {
            get { return false; }
        }
    }
}
