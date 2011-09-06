using System.Collections.Generic;

namespace IndiaTango.Models
{
    /// <summary>
    /// Interface providing data reading capabilities.
    /// </summary>
    public interface IDataReader
    {
        /// <summary>
        /// Reads in a list of sensors from a given data source.
        /// </summary>
        /// <returns>The List of Sensors generated.</returns>
        List<Sensor> ReadSensors();

        /// <summary>
        /// Reads in the Site information from a given data source.
        /// </summary>
        /// <returns>The Site object created.</returns>
        Site ReadBuoy();
    }
}
