using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IndiaTango.Models
{
    /// <summary>
    /// A variable for a sensor, used in formula calibration
    /// </summary>
    public class SensorVariable : IComparable<SensorVariable>
    {
        private Sensor _sensor;
        private string _variableName;

        public SensorVariable(Sensor sensor, string variableName)
        {
            if (sensor == null)
                throw new ArgumentNullException("Sensor cannot be null.");

            if (String.IsNullOrEmpty(variableName))
                throw new ArgumentNullException("variableName cannot be null.");

            _sensor = sensor;
            _variableName = variableName;
        }

        /// <summary>
        /// The sensor the variable name belongs to
        /// </summary>
        public Sensor Sensor
        {
            get { return _sensor; }
        }

        /// <summary>
        /// The variable of the sensor
        /// </summary>
        public string VariableName
        {
            get { return _variableName; }
        }

        public int CompareTo(SensorVariable other)
        {
            if(VariableName.Length == other.VariableName.Length)
                return VariableName.CompareTo(other.VariableName);
            return VariableName.Length - other.VariableName.Length;
        }

        public override string ToString()
		{
			return VariableName;
		}

        /// <summary>
        /// Creates a list of sensor variables from a list of sensors
        /// </summary>
        /// <param name="sensors">The sensors to create variables for</param>
        /// <returns>The list of sensor variables created</returns>
        public static List<SensorVariable> CreateSensorVariablesFromSensors(List<Sensor> sensors)
        {
            List<SensorVariable> variables = new List<SensorVariable>();
            int i = 0;

            foreach (Sensor sensor in sensors)
            {
                variables.Add(new SensorVariable(sensor,
                    (i > 25 ? Convert.ToChar((int)Math.Floor(i / 26d) + 96) + "" : "") +
                    "" + Convert.ToChar((int)(i % 26) + 97)));
                i++;
            }

            return variables;
        }

        /// <summary>
        /// Gets the sensors from the sensor variables provided
        /// </summary>
        /// <param name="sensorVariables">The sensor variables to get sensors from</param>
        /// <returns>The list of sensors</returns>
        public static List<Sensor> CreateSensorsFromSensorVariables(List<SensorVariable> sensorVariables)
        {
            List<Sensor> sensors = new List<Sensor>();

            foreach (SensorVariable sensor in sensorVariables)
                sensors.Add(sensor.Sensor);

            return sensors;
        }

    }
}
