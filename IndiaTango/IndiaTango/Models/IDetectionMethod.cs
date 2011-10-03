using System.Collections.Generic;
using System.Windows.Controls;

namespace IndiaTango.Models
{
    public interface IDetectionMethod
    {
        IDetectionMethod This { get; }

        List<string> GetDetectedValues();

        bool HasSettings { get; }

        Grid SettingsGrid { get; }
    }
}
