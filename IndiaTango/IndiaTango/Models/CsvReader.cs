using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace IndiaTango.Models
{
    public class CSVReader : IDataReader
    {
        private string _filename;
        private Sensor[] sensors;
        public event ReaderProgressChanged ProgressChanged;

        public CSVReader(string fileName)
        {
            if (!fileName.EndsWith(".csv"))
                throw new ArgumentException("Can't tell if this is a csv (filetype not .csv)");

            if (!File.Exists(fileName))
                throw new ArgumentException("File wasn't found");

            _filename = fileName;

        	//ProgressChanged += OnProgressChanged;
        }

        public List<Sensor> ReadSensors()
        {
            return ReadSensors(null);
        }

        public List<Sensor> ReadSensors(BackgroundWorker asyncWorker)
        {
        	if (sensors != null)
                return sensors.ToList();

            sensors = new Sensor[0];

            try
            {
                using (var sr = new StreamReader(_filename))
                {
                    var linesInFile = File.ReadLines(_filename).Count();
					var linesRead = 0d;
                	var progressValue = 0;
					var oldProgressValue = 0;

                    var sensorNamesString = sr.ReadLine();

                    String[] sensorNames;
                    if (sensorNamesString != null)
                        sensorNames = sensorNamesString.Split(',');
                    else
                        throw new FormatException("Couldn't get the sensor names from the csv");

                    //First two are the time stamp
                    sensors = new Sensor[sensorNames.Length - 2];

                    for (int i = 2; i < sensorNames.Length; i++)
                    {
                        if (asyncWorker != null && asyncWorker.CancellationPending)
                            return null;

                        sensors[i - 2] = new Sensor(sensorNames[i], null);
                        sensors[i - 2].AddState(new SensorState(DateTime.Now));
                    }


                    string readLine = null;
                    while ((readLine = sr.ReadLine()) != null)
                    {
                        if (asyncWorker != null && asyncWorker.CancellationPending)
                            return null;

                    	linesRead++;
                    	progressValue = (int) (linesRead/linesInFile*100);

						//We now only trigger the event every time the return value increases.
						//There seems to be a crazy overhead on firing events, so we only fire it when it matters (when that value has changed)
						//Comment out 'OnProgressChanged()' below to see speed improvements if we dont fire events at all. Times can be seen in the log file or debug window
						//I think we can easily afford to fire only 100 events, rather than ~50000 :) Speedy speed!!!
                    	if(progressValue > oldProgressValue)
                    	{
                    		OnProgressChanged((object) this,new ReaderProgressChangedArgs(progressValue));
                    		oldProgressValue = progressValue;
                    	}

						var values = readLine.Split(',');
						if (values.Length != sensorNames.Length)
							throw new FormatException("Number of values mismatch from the number of sensors");
						var timeStamp = DateTime.Parse(values[0] + " " + values[1]);

						for (int i = 2; i < values.Length; i++)
						{
							if (!String.IsNullOrWhiteSpace(values[i]))
							{
								try
								{
									sensors[i - 2].CurrentState.Values.Add(timeStamp, float.Parse(values[i]));
								}
								catch (Exception e)
								{
								}
							}
						}
                    }
                }
            }
            catch (IOException e)
            {
                Common.ShowMessageBox("An Error Occured", e.Message, false, true);
                return null;
            }
            
            return sensors.ToList();
        }

        public Site ReadBuoy()
        {
            throw new NotImplementedException();
        }

        void OnProgressChanged(object o, ReaderProgressChangedArgs e)
        {
            if (ProgressChanged != null)
                ProgressChanged(o, e);
        }
    }

    public class ReaderProgressChangedArgs : EventArgs
    {
        public readonly int Progress;

        public ReaderProgressChangedArgs(int progress)
        {
            Progress = progress;
        }
    }

    public delegate void ReaderProgressChanged(object o, ReaderProgressChangedArgs e);
}