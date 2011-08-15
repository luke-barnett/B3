using System;
using System.Collections.Generic;
using System.IO;

namespace IndiaTango.Models
{
    public class CsvReader : IDataReader
    {
        private List<Sensor> _sensors = new List<Sensor>();

        public CsvReader(string fileName)
        {
            try
            {
                var reader = new StreamReader(fileName);
                var line = reader.ReadLine();
                var parts = line.Split(',');
                
                //first element is date, second is time
                for (var i = 2; i < parts.Length; i++)
                {
                    _sensors.Add(new Sensor(parts[i], "C"));
                }

                while (reader.Peek() != -1)
                {
                    line = reader.ReadLine();
                    parts = line.Split(',');
                    var date = parts[0].Split('/');
                    var time = parts[1].Split(':');
                    var timeStamp = new DateTime(Int32.Parse(date[2]), Int32.Parse(date[1]), Int32.Parse(date[0]),Int32.Parse(time[0]),Int32.Parse(time[1]),0);
                    var sState = new SensorState(timeStamp);
                    //first element is date, second is time
                    for (var i = 2; i < parts.Length; i++)
                    {
                        //_sensors[i]
                    }
                }


            }
            catch (Exception e)
            {
                throw e;
            }

        }

        public List<Sensor> ReadSensors()
        {
            return null;
        }

        public object ReadBuoy()
        {
            throw new NotImplementedException();
        }
    }
}