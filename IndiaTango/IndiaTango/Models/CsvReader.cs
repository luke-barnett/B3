using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace IndiaTango.Models
{
    public class CSVReader : IDataReader
    {
        private readonly string _filename;
        private Sensor[] _sensors;
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
            return ReadSensors(null, null);
        }

        public List<Sensor> ReadSensors(BackgroundWorker asyncWorker, Dataset owner)
        {
            if (_sensors != null)
                return _sensors.ToList();

            var linesInFile = File.ReadLines(_filename).Count();
            var linesRead = 0d;
            var oldProgresValue = 0;

            using (var reader = new StreamReader(_filename))
            {
                var csvHeaderLine = reader.ReadLine();
                if (csvHeaderLine == null)
                    throw new FileFormatException("Couldn't read the header line!");

                linesRead++;

                var headers = csvHeaderLine.Split(',');

                var dateTimeComponents = new List<DateTimeComponent>();
                var sensorIndexers = new List<SensorIndexer>();

                foreach (var header in headers)
                {
                    DateTimeComponentType dateTimeComponent;
                    var cleansedHeader = header.Replace(" ", "").Replace("-", "").Replace("/", "").Replace(":", "");
                    var isDateTimeComponent = Enum.TryParse(cleansedHeader, out dateTimeComponent);
                    Debug.Print("{0} is a dateTimeComponent: {1}", header, isDateTimeComponent);
                    if (isDateTimeComponent)
                    {
                        dateTimeComponents.Add(new DateTimeComponent
                        {
                            Index = Array.IndexOf(headers, header),
                            Type = dateTimeComponent
                        });
                    }
                    else
                    {
                        sensorIndexers.Add(new SensorIndexer
                        {
                            Index = Array.IndexOf(headers, header),
                            Sensor = new Sensor(header, null, owner)
                        });
                    }
                }

                string lineRead;
                while ((lineRead = reader.ReadLine()) != null)
                {
                    if (asyncWorker != null && asyncWorker.CancellationPending)
                        return null;
                    linesRead++;

                    var progress = (int)(linesRead / linesInFile * 100);

                    if (progress > oldProgresValue)
                    {
                        OnProgressChanged(this, new ReaderProgressChangedArgs(progress));
                        oldProgresValue = progress;
                    }

                    var lineComponents = lineRead.Split(',');

                    var cleansedLineComponenets = lineRead.Split(',');
                    for (var i = 0; i < cleansedLineComponenets.Length; i++)
                    {
                        cleansedLineComponenets[i] = cleansedLineComponenets[i].Replace(" ", "").Replace("-", "").Replace("/", "").Replace(":", "");
                    }

                    var dateTime = DateTime.MinValue;
                    var hasYear = false;
                    var hasMonth = false;
                    var hasDay = false;
                    var hasHour = false;
                    var hasMinute = false;
                    foreach (var dateTimeComponent in dateTimeComponents)
                    {
                        if (dateTimeComponent.Index > cleansedLineComponenets.Length)
                            throw new FileFormatException("There aren't enough values for the date-time components");
                        try
                        {
                            #region Date Parsers
                            if (dateTimeComponent.Type == DateTimeComponentType.DD)
                            {
                                dateTime = new DateTime(dateTime.Year, dateTime.Month, int.Parse(cleansedLineComponenets[dateTimeComponent.Index]), dateTime.Hour, dateTime.Minute, 0);
                                hasDay = true;
                            }
                            else if (dateTimeComponent.Type == DateTimeComponentType.DDMM)
                            {
                                dateTime = new DateTime(dateTime.Year, int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 2)), dateTime.Hour, dateTime.Minute, 0);
                                hasDay = true;
                                hasMonth = true;
                            }
                            else if (dateTimeComponent.Type == DateTimeComponentType.DDMMYYYY)
                            {
                                dateTime = new DateTime(int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(4)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(2, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 2)), dateTime.Hour, dateTime.Minute, 0);
                                hasDay = true;
                                hasMonth = true;
                                hasYear = true;
                            }
                            else if (dateTimeComponent.Type == DateTimeComponentType.DDMMYYYYhhmm)
                            {
                                dateTime = new DateTime(int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(4, 4)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(2, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(8, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(10, 2)), 0);
                                hasDay = true;
                                hasMonth = true;
                                hasYear = true;
                                hasHour = true;
                                hasMinute = true;
                            }
                            else if (dateTimeComponent.Type == DateTimeComponentType.MM)
                            {
                                dateTime = new DateTime(dateTime.Year, int.Parse(cleansedLineComponenets[dateTimeComponent.Index]), dateTime.Day, dateTime.Hour, dateTime.Minute, 0);
                                hasMonth = true;
                            }
                            else if (dateTimeComponent.Type == DateTimeComponentType.MMDD)
                            {
                                dateTime = new DateTime(dateTime.Year, int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(2)), dateTime.Hour, dateTime.Minute, 0);
                                hasMonth = true;
                                hasDay = true;
                            }
                            else if (dateTimeComponent.Type == DateTimeComponentType.MMDDYYYY)
                            {
                                dateTime = new DateTime(int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(4)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(2, 2)), dateTime.Hour, dateTime.Minute, 0);
                                hasMonth = true;
                                hasDay = true;
                                hasYear = true;
                            }
                            else if (dateTimeComponent.Type == DateTimeComponentType.MMDDYYYYhhmm)
                            {
                                dateTime = new DateTime(int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(4, 4)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(2, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(8, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(10, 2)), 0);
                                hasMonth = true;
                                hasDay = true;
                                hasYear = true;
                                hasHour = true;
                                hasMinute = true;
                            }
                            else if (dateTimeComponent.Type == DateTimeComponentType.YYYY)
                            {
                                dateTime = new DateTime(int.Parse(cleansedLineComponenets[dateTimeComponent.Index]), dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0);
                                hasYear = true;
                            }
                            else if (dateTimeComponent.Type == DateTimeComponentType.YYYYDDMM)
                            {
                                dateTime = new DateTime(int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 4)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(6)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(4, 2)), dateTime.Hour, dateTime.Minute, 0);
                                hasYear = true;
                                hasDay = true;
                                hasMonth = true;
                            }
                            else if (dateTimeComponent.Type == DateTimeComponentType.YYYYDDMMhh)
                            {
                                dateTime = new DateTime(int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 4)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(6, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(4, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(8)), dateTime.Minute, 0);
                                hasYear = true;
                                hasDay = true;
                                hasMonth = true;
                                hasHour = true;
                            }
                            else if (dateTimeComponent.Type == DateTimeComponentType.YYYYDDMMhhmm)
                            {
                                dateTime = new DateTime(int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 4)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(6, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(4, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(8, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(10)), 0);
                                hasYear = true;
                                hasDay = true;
                                hasMonth = true;
                                hasHour = true;
                                hasMinute = true;
                            }
                            else if (dateTimeComponent.Type == DateTimeComponentType.YYYYMM)
                            {
                                dateTime = new DateTime(int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 4)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(4)), dateTime.Day, dateTime.Hour, dateTime.Minute, 0);
                                hasYear = true;
                                hasMonth = true;
                            }
                            else if (dateTimeComponent.Type == DateTimeComponentType.YYYYMMDD)
                            {
                                dateTime = new DateTime(int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 4)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(4, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(6)), dateTime.Hour, dateTime.Minute, 0);
                                hasYear = true;
                                hasMonth = true;
                                hasDay = true;
                            }
                            else if (dateTimeComponent.Type == DateTimeComponentType.YYYYMMDDhh)
                            {
                                dateTime = new DateTime(int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 4)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(4, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(6, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(8)), dateTime.Minute, 0);
                                hasYear = true;
                                hasMonth = true;
                                hasDay = true;
                                hasHour = true;
                            }
                            else if (dateTimeComponent.Type == DateTimeComponentType.YYYYMMDDhhmm)
                            {
                                dateTime = new DateTime(int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 4)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(4, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(6, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(8, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(10)), 0);
                                hasYear = true;
                                hasMonth = true;
                                hasDay = true;
                                hasHour = true;
                            }
                            else if (dateTimeComponent.Type == DateTimeComponentType.hh)
                            {
                                dateTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, int.Parse(cleansedLineComponenets[dateTimeComponent.Index]), dateTime.Minute, 0);
                                hasHour = true;
                            }
                            else if (dateTimeComponent.Type == DateTimeComponentType.hhmm)
                            {
                                dateTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(2)), 0);
                                hasHour = true;
                                hasMinute = true;
                            }
                            else if (dateTimeComponent.Type == DateTimeComponentType.mm)
                            {
                                dateTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, int.Parse(cleansedLineComponenets[dateTimeComponent.Index]), 0);
                                hasMinute = true;
                            }
                            #endregion
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            throw new FileFormatException(string.Format("The date time component for {0} on line {1} is not formatted correctly\r\n\nWe have read it as:\r\n{2}", headers[dateTimeComponent.Index], linesRead, lineComponents[dateTimeComponent.Index]));
                        }

                    }
                    if (!hasYear || !hasMonth || !hasDay || !hasHour || !hasMinute)
                    {
                        var errorMessage = "Date wasn't complete\r\n";

                        if (!hasYear)
                            errorMessage += "\r\nMissing Year!";
                        if (!hasMonth)
                            errorMessage += "\r\nMissing Month!";
                        if (!hasDay)
                            errorMessage += "\r\nMissing Day!";
                        if (!hasHour)
                            errorMessage += "\r\nMissing Hour!";
                        if (!hasMinute)
                            errorMessage += "\r\nMissing Minute!";

                        errorMessage += "\r\nPlease reformat for the ISO 8601 standard\r\n\nDate Headers Recognized:";
                        errorMessage = dateTimeComponents.Aggregate(errorMessage,
                                                                    (current, component) =>
                                                                    string.Format("{0}\r\n{1}", current, headers[component.Index]));

                        errorMessage += "\r\n\r\nFailing Line (Line Number " + linesRead + "):\r\n" + lineRead;
                        throw new FileFormatException(errorMessage);
                    }

                    foreach (var sensorIndexer in sensorIndexers)
                    {
                        if (sensorIndexer.Index > lineComponents.Length)
                            throw new FileFormatException("There aren't enough values for all the sensors");

                        float value;
                        if (float.TryParse(lineComponents[sensorIndexer.Index], out value))
                            sensorIndexer.Sensor.RawData.Values[dateTime] = value;
                    }
                }

                //Convert SensorIndexes to Array
                _sensors = sensorIndexers.OrderBy(x => x.Index).Select(x => x.Sensor).ToArray();
            }

            if (_sensors == null)
                throw new FileFormatException("No sensors were read! File is of bad format");
            return _sensors.ToList();
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

    public enum DateTimeComponentType
    {
        YYYY,
        MM,
        DD,
        hh,
        mm,
        YYYYMM,
        YYYYMMDD,
        YYYYMMDDhhmm,
        YYYYMMDDhh,
        YYYYDDMM,
        YYYYDDMMhhmm,
        YYYYDDMMhh,
        MMDD,
        DDMM,
        DDMMYYYY,
        DDMMYYYYhhmm,
        MMDDYYYY,
        MMDDYYYYhhmm,
        hhmm
    }

    public class DateTimeComponent
    {
        public int Index;
        public DateTimeComponentType Type;
    }

    public class SensorIndexer
    {
        public int Index;
        public Sensor Sensor;
    }
}