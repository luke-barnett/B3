using System;
using System.Collections.Generic;

namespace IndiaTango.Models
{
    public interface IDataReader
    {
        /// <summary>
        /// Reads in a list of sensors from the data source
        /// </summary>
        /// <returns>The List of sensors it created</returns>
        List<Sensor> ReadSensors();

        /// <summary>
        /// Reads in the Buoy information from the data source
        /// </summary>
        /// <returns>The buoy it created</returns>
        Object ReadBuoy();
    }
}
