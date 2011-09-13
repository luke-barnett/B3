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

                    // First cell of column headings is a valid date/time component
                    var isIndividualDateComponents = (sensorNames.Length >= 5 &&
                                                      (sensorNames[0] == "dd" || sensorNames[0] == "yyyy" ||
                                                       sensorNames[0] == "mm"));
                    int startOffset = isIndividualDateComponents ? 5 : 2;

                    //First two are the time stamp
                    sensors = new Sensor[sensorNames.Length - startOffset];

                    for (int i = startOffset; i < sensorNames.Length; i++)
                    {
                        if (asyncWorker != null && asyncWorker.CancellationPending)
                            return null;

                        sensors[i - startOffset] = new Sensor(sensorNames[i], null);
                        sensors[i - startOffset].AddState(new SensorState(DateTime.Now));
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

                        var components = new string[5];

                        if(isIndividualDateComponents)
                        {
                            // Normalise to form dd/mm/yyy hh:mm
                            for (int i = 0; i < 5; i++)
                            {
                                switch(sensorNames[i])
                                {
                                    case "dd":
                                        components[0] = values[i];
                                        break;
                                    case "mm":
                                        components[1] = values[i];
                                        break;
                                    case "yyyy":
                                        components[2] = values[i];
                                        break;
                                    case "hh":
                                        components[3] = values[i];
                                        break;
                                    case "nn":
                                        components[4] = values[i];
                                        break;
                                }
                            }
                        }

                        var timeStamp = DateTime.Parse((isIndividualDateComponents) ? components[0] + "/" + components[1] + "/" + components[2] + " " + components[3] + ":" + components[4] : values[0] + " " + values[1]);

						for (int i = startOffset; i < values.Length; i++)
						{
							if (!String.IsNullOrWhiteSpace(values[i]))
							{
								try
								{
									sensors[i - startOffset].CurrentState.Values.Add(timeStamp, float.Parse(values[i]));
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