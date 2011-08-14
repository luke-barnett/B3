using System;
using System.Collections.Generic;
using System.Linq;

namespace IndiaTango.Models
{
    public class DataStringReader : IDataReader
    {
        private string sensorInformation;
        private string buoyInformation;

        /// <summary>
        /// Creates a new DataStringReader object that reads basic buoy and sensor information from a string
        /// </summary>
        /// <param name="inputString">The input string to gather the information from</param>
        public DataStringReader(string inputString)
        {
            //First split the data into the two expected sections
            var parts = inputString.Split(',');
            //If we don't have two we may as well stop now
            if (parts.Length != 2) throw new FormatException("Could not split the data correctly");
            //For each section work out what type it is
            foreach (var part in parts)
            {
                //Split it into it's sections
                var partsections = part.Split(':');
                //If we don't have enough sections then something is wrong
                if(partsections.Length != 2) throw new FormatException(String.Format("Incorrect number of parts in {0}. Could not determine between Buoy and Sensor information", part));
                //Check it against the two possible outcomes
                if (partsections[0].CompareTo("Buoy") == 0)
                {
                    //If we have already set a value for this then we have something wrong with the data
                    if (String.IsNullOrEmpty(buoyInformation))
                        buoyInformation = partsections[1];
                    else
                        throw new FormatException(String.Format("Multiple Buoy data in {0}", inputString));
                }
                else if (partsections[0].CompareTo("Sensors") == 0)
                {
                    //If we have already set a value for this then we have something wrong with the data
                    if(String.IsNullOrEmpty(sensorInformation))
                        sensorInformation = partsections[1];
                    else
                        throw new FormatException(String.Format("Multiple sensors data in {0}", inputString));
                }
                else
                    //If it's not something that we recognise then we should stop processing
                    throw new FormatException(String.Format("Couldn't match the type of {0} to a Buoy or Sensor type", partsections[0]));
            }
        }

        public List<Sensor> ReadSensors()
        {
            var stringSensors = sensorInformation.Split('&');

            foreach (string sensor in stringSensors)
            {
                if (!sensor.EndsWith("]"))
                    throw new FormatException(
                        "Sensor readings are malformed; list must start with '[' and end with ']'.");

                var sensorValues = sensor.Split('[');
            }

            return stringSensors.Select(stringSensor => new Sensor()).ToList();
        }

        public object ReadBuoy()
        {
            throw new NotImplementedException();
        }
    }
}
