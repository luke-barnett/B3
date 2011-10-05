using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IndiaTango.Models
{
    public class SensorVariable
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

        public Sensor Sensor
        {
            get { return _sensor; }
        }

        public string VariableName
        {
            get { return _variableName; }
        }

        public static List<SensorVariable> CreateSensorVariablesFromSensors(List<Sensor> sensors)
        {
            List<SensorVariable> variables = new List<SensorVariable>();
            int i = 0;
            string var;

            foreach (Sensor sensor in sensors)
            {
                variables.Add(new SensorVariable(sensor,
                    (i > 25 ? Convert.ToChar((int)Math.Floor(i / 26d) + 96) + "" : "") +
                    "" + Convert.ToChar((int)(i % 26) + 97)));
                i++;
            }

            return variables;
        }

        public static List<Sensor> CreateSensorsFromSensorVariables(List<SensorVariable> sensorVariables)
        {
            List<Sensor> sensors = new List<Sensor>();

            foreach (SensorVariable sensor in sensorVariables)
                sensors.Add(sensor.Sensor);

            return sensors;
        }

    }
}
