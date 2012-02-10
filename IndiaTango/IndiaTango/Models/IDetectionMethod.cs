using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Visiblox.Charts;

namespace IndiaTango.Models
{
    /// <summary>
    /// Interface for detection methods
    /// </summary>
    public interface IDetectionMethod
    {
        /// <summary>
        /// The Name of the Detection Method
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The abbreviation for the detection method
        /// </summary>
        string Abbreviation { get; }

        /// <summary>
        /// The Detection Method itself (must go deeper)
        /// </summary>
        IDetectionMethod This { get; }

        /// <summary>
        /// The list of erroneous values the method finds
        /// </summary>
        /// <param name="sensorToCheck">The sensor to look within</param>
        /// <returns>The list of erroneous values</returns>
        List<ErroneousValue> GetDetectedValues(Sensor sensorToCheck);

        /// <summary>
        /// Does this method have additional settings?
        /// </summary>
        bool HasSettings { get; }

        /// <summary>
        /// The layout Grid for the methods settings
        /// </summary>
        Grid SettingsGrid { get; }

        /// <summary>
        /// Does this method give anything to graph?
        /// </summary>
        bool HasGraphableSeries { get; }

        /// <summary>
        /// Checks if a single value passes the detection method or not
        /// </summary>
        /// <param name="sensor">The sensor to use</param>
        /// <param name="timeStamp">The timestamp of the value</param>
        /// <returns>If the value gets past the detection method or not</returns>
        bool CheckIndividualValue(Sensor sensor, DateTime timeStamp);

        /// <summary>
        /// The list of series that relate to this detection method
        /// <param name="sensorToBaseOn">The sensor to base the series on</param>
        /// </summary>
        List<LineSeries> GraphableSeries(Sensor sensorToBaseOn, DateTime startDate, DateTime endDate);

        /// <summary>
        /// The list of series that relate to this detection method
        /// </summary>
        List<LineSeries> GraphableSeries(DateTime startDate, DateTime endDate);

        /// <summary>
        /// The detection methods children
        /// </summary>
        List<IDetectionMethod> Children { get; }

        /// <summary>
        /// If the method is enabled or not
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// The list box to put detected values into
        /// </summary>
        ListBox ListBox { get; set; }

        /// <summary>
        /// The list of available sensors to use for graphing
        /// </summary>
        Sensor[] SensorOptions { set; }

        /// <summary>
        /// A summary string about the detection method
        /// </summary>
        string About { get; }

        /// <summary>
        /// The default reason number this detection method relates to
        /// </summary>
        int DefaultReasonNumber { get; }
    }
}
