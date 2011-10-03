using System.Collections.Generic;

namespace IndiaTango.Models
{
    public interface IDetectionMethod
    {
        IDetectionMethod This { get; }

        List<string> GetDetectedValues();
    }
}
