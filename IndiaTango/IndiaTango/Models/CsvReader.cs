using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IndiaTango.Models
{
    public class CSVReader : IDataReader
    {
        private string _filename;
        private Sensor[] sensors;

        public CSVReader(string fileName)
        {
            if (!fileName.EndsWith(".csv"))
                throw new ArgumentException("Can't tell if this is a csv (filetype not .csv)");

            if (!File.Exists(fileName))
                throw new ArgumentException("File wasn't found");

            _filename = fileName;
        }

        public List<Sensor> ReadSensors()
        {
            if (sensors != null)
                return sensors.ToList();

            sensors = new Sensor[0];
            using (var sr = new StreamReader(_filename))
            {
                var sensorNamesString = sr.ReadLine();

                String[] sensorNames;
                if(sensorNamesString != null)
                    sensorNames = sensorNamesString.Split(',');
                else
                    throw new FormatException("Couldn't get the sensor names from the csv");
                
                //First two are the time stamp
                sensors = new Sensor[sensorNames.Length - 2];

                for (int i = 2; i < sensorNames.Length; i++ )
                {
                    sensors[i - 2] = new Sensor(sensorNames[i], null);
                    sensors[i -2].AddState(new SensorState(DateTime.Now));
                }


                String readLine = null;
                while((readLine = sr.ReadLine()) != null)
                {
                    var values = readLine.Split(',');
                    if(values.Length != sensorNames.Length)
                        throw new FormatException("Number of values mismatch from the number of sensors");
                    var timeStamp = DateTime.Parse(values[0] + " " + values[1]);

                    for (int i = 2; i < values.Length; i++)
                    {
                        try
                        {
                            sensors[i - 2].CurrentState.Values.Add(new DataValue(timeStamp, float.Parse(values[i])));
                        }
                        catch(Exception e)
                        {
                        }
                    }
                }

            }
            
            return sensors.ToList();
        }

        public object ReadBuoy()
        {
            throw new NotImplementedException();
        }
    }
}