using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Ionic.Zip;
using Ionic.Zlib;
using ProtoBuf;

namespace IndiaTango.Models
{
    [Serializable]
    [ProtoContract]
    public class Dataset : INotifyPropertyChanged
    {
        private Site _site;
        private DateTime _startTimeStamp;
        private DateTime _endTimeStamp;
        //[ProtoMember(4)]
        private List<Sensor> _sensors;
        [ProtoMember(6)]
        private int _expectedDataPointCount;
        [ProtoMember(7)]
        private int _actualDataPointCount;
        private int _dataInterval;
        private int _lowestYearLoaded;
        private int _highestYearLoaded;

        public Dataset() { } //For Protobuf-net

        /// <summary>
        /// Creates a new dataset with a specified start and end timestamp
        /// </summary>
        /// <param name="site">The Site that the dataset came from</param>
        public Dataset(Site site)
        {
            _site = site;
            _startTimeStamp = DateTime.MinValue;
            _endTimeStamp = DateTime.MinValue;
            _sensors = new List<Sensor>();
        }

        /// <summary>
        /// Creates a new dataset from a list of sensors. Start and end timestamp will be dynamically created
        /// </summary>
        /// <param name="site">The Site that the dataset came from</param>
        /// <param name="sensors">List of sensors belonging to the dataset</param>
        public Dataset(Site site, List<Sensor> sensors)
        {
            if (sensors == null)
                throw new ArgumentException("Please provide a list of sensors that belong to this site.");
            if (sensors.Count == 0)
                throw new ArgumentException("Sensor list must contain at least one sensor.");

            _site = site;
            Sensors = sensors; //To trigger setter
        }

        /// <summary>
        /// Gets and sets the Site that this dataset came from
        /// </summary>
        ///
        //[ProtoMember(1)]
        public Site Site
        {
            get { return _site; }
            set { _site = value; }
        }

        /// <summary>
        /// Returns the start time stamp for this dataset
        /// </summary>
        [ProtoMember(2)]
        public DateTime StartTimeStamp
        {
            get { return _startTimeStamp; }
            private set { _startTimeStamp = value; }
        }

        public DateTime StartYear
        {
            get
            {
                return new DateTime(StartTimeStamp.Year, 1, 1);
            }
        }

        /// <summary>
        /// Returns the end time stamp for this dataset
        /// </summary>
        [ProtoMember(3)]
        public DateTime EndTimeStamp
        {
            get { return _endTimeStamp; }
            private set { _endTimeStamp = value; }
        }

        public DateTime EndYear
        {
            get
            {
                return new DateTime(EndTimeStamp.Year, 1, 1);
            }
        }

        /// <summary>
        /// Returns the list of sensors for this dataset
        /// </summary>
        public List<Sensor> Sensors
        {
            get { return _sensors; }
            set
            {
                if (value == null)
                    throw new ArgumentException("Sensors cannot be null");

                _sensors = value;

                CalculateDataSetValues();
            }
        }

        /// <summary>
        /// Adds a sensor to the list of sensors
        /// </summary>
        /// <param name="sensor">The sensor to be added to the list of sensors</param>
        public void AddSensor(Sensor sensor)
        {
            if (sensor == null)
                throw new ArgumentException("Sensor cannot be null");

            // Force an update of the start and end timestamps - very important!
            var sensorList = Sensors;
            sensorList.Add(sensor);
            Sensors = sensorList;
        }

        /// <summary>
        /// Returns the time interval in minutes the data points are placed at
        /// </summary>
        [ProtoMember(5)]
        public int DataInterval
        {
            get { return _dataInterval; }
            set
            {
                if (value >= 0)
                    _dataInterval = value;
                else
                    throw new ArgumentException("Data interval must be greater than 0.");
            }
        }

        /// <summary>
        /// Returns the actual number of data rows in this data set
        /// </summary>
        public int ActualDataPointCount
        {
            get { return _actualDataPointCount; }
        }

        /// <summary>
        /// Returns the expected number of data rows in this data set, if empty rows were included
        /// </summary>
        public int ExpectedDataPointCount
        {
            get { return _expectedDataPointCount; }
        }

        /// <summary>
        /// This method reads as 'move sensor A to the spot occupied by sensor B'. It moves sensor A to the spot where sensor B was, and moves sensor B one spot below it.
        /// </summary>
        /// <param name="a">The sensor to be moved.</param>
        /// <param name="b">The sensor whose spot will become sensor A's new location.</param>
        public void MoveSensorTo(Sensor a, Sensor b)
        {
            if (a.Equals(b))
                return;

            var newIndex = 0;

            var goingDown = Sensors.IndexOf(a) < Sensors.IndexOf(b);

            if (goingDown)
                newIndex = Sensors.IndexOf(b); // Going down - use current index in the list

            Sensors.Remove(a);

            if (!goingDown)
                newIndex = Sensors.IndexOf(b);

            if (newIndex < Sensors.Count)
                Sensors.Insert(newIndex, a);
            else
                Sensors.Add(a);
        }

        public void CalculateDataSetValues()
        {
            if (Sensors.Count > 0 && Sensors[0] != null && Sensors[0].CurrentState != null)
            {
                var intervalMap = new Dictionary<int, int>();
                var prevDate = DateTime.MinValue;
                var currentHighest = new KeyValuePair<int, int>();


                foreach (var date in Sensors[0].CurrentState.Values.Keys)
                {
                    var interval = (int)(date - prevDate).TotalMinutes;

                    if (intervalMap.ContainsKey(interval))
                        intervalMap[interval]++;
                    else
                        intervalMap.Add(interval, 1);

                    prevDate = date;
                }

                foreach (var pair in intervalMap)
                    if (pair.Value > currentHighest.Value)
                        currentHighest = pair;


                _dataInterval = currentHighest.Key;
            }

            foreach (Sensor sensor in _sensors)
            {
                //Update the actual data point count.
                if (sensor.CurrentState != null)
                    _actualDataPointCount = Math.Max(sensor.CurrentState.Values.Count, _actualDataPointCount);

                //Set the start and end time dynamically
                if (sensor.CurrentState != null && sensor.CurrentState.Values.Count > 0)
                {
                    var timesArray = new DateTime[sensor.CurrentState.Values.Count];
                    sensor.CurrentState.Values.Keys.CopyTo(timesArray, 0);
                    var timesList = new List<DateTime>(timesArray);
                    timesList.Sort();

                    if (_startTimeStamp == DateTime.MinValue || timesList[0] < _startTimeStamp)
                        _startTimeStamp = timesList[0];

                    if (_startTimeStamp == DateTime.MinValue || timesList[timesList.Count - 1] > _endTimeStamp)
                        _endTimeStamp = timesList[timesList.Count - 1];
                }
            }

            if (_sensors.Count > 0)
                _expectedDataPointCount = (int)Math.Floor(EndTimeStamp.Subtract(StartTimeStamp).TotalMinutes / DataInterval) + 1;
        }

        public string IdentifiableName
        {
            get { return (Site != null && !string.IsNullOrWhiteSpace(Site.Name)) ? Site.Name : Common.UnknownSite; }
        }

        public string SaveLocation
        {
            get
            {
                return Path.Combine(Common.DatasetSaveLocation, string.Format("{0} - {1}.b3", _site.Id, _site.Name));
            }
        }

        public int LowestYearLoaded
        {
            get { return _lowestYearLoaded; }
            set
            {
                _lowestYearLoaded = value;
                OnPropertyChanged("LowestYearLoaded");
            }
        }

        public int HighestYearLoaded
        {
            get { return _highestYearLoaded; }
            set
            {
                _highestYearLoaded = value;
                OnPropertyChanged("HighestYearLoaded");
            }
        }

        public static string[] GetAllDataSetFileNames()
        {
            return Directory.Exists(Common.DatasetSaveLocation) ? Directory.GetFiles(Common.DatasetSaveLocation, "*.b3") : new string[0];
        }

        public static Dataset LoadDataSet(string fileName)
        {
            EventLogger.LogInfo(null, "Loading", "Started loading from file: " + fileName);
            try
            {
                return LoadDataSetFromFile(fileName);
            }
            catch (Exception e)
            {
                if (File.Exists(fileName + ".backup"))
                {
                    try
                    {
                        Common.ShowMessageBox("Failed to load site",
                                              "We were unable to read the site file. However a backup was found, now trying to load from backup",
                                              false, false);
                        return LoadDataSetFromFile(fileName + ".backup");
                    }
                    catch (Exception e2)
                    {
                        Common.ShowMessageBoxWithException("Failed to load site",
                                                           "We were unable to read the backup site file as it was corrupted.",
                                                           false, true, e2);
                    }

                }

                Common.ShowMessageBoxWithException("Failed to load site",
                                                   "We were unable to read the site file as it was corrupted", false,
                                                   true, e);
            }
            return null;
        }

        private static Dataset LoadDataSetFromFile(string filename)
        {
            /*
            using (var file = File.OpenRead(filename))
                return Serializer.Deserialize<Dataset>(file);
             * */

            if (!ZipFile.IsZipFile(filename))
            {
                Common.ShowMessageBox("File in old format",
                                      "We are unable to load in your site as it's not in the updated format\r\nPlease create a new site and import your data again\r\n\n(Note %APPDATA%/B3/Backups/Exports should have a copy of your latest data)",
                                      false, false);
                return null;
            }

            using (var zip = ZipFile.Read(filename))
            {
                var datasetStream = new MemoryStream();
                zip.Entries.First(x => x.FileName == "dataset").Extract(datasetStream);
                datasetStream.Position = 0;

                var dataset = Serializer.Deserialize<Dataset>(datasetStream);

                var siteStream = new MemoryStream();
                zip.Entries.First(x => x.FileName == "site").Extract(siteStream);
                siteStream.Position = 0;

                dataset.Site = Serializer.Deserialize<Site>(siteStream);

                var sensors = zip.Entries.Where(x => x.FileName.EndsWith("metadata")).ToArray();

                dataset.Sensors = new List<Sensor>();

                foreach (var sensor in sensors)
                {
                    var sensorStream = new MemoryStream();
                    sensor.Extract(sensorStream);
                    sensorStream.Position = 0;
                    var sensorobject = Serializer.Deserialize<Sensor>(sensorStream);
                    sensorobject.Owner = dataset;

                    var lastestYear =
                        zip.Entries.FirstOrDefault(
                            x =>
                            x.FileName.Contains(sensorobject.Name.GetHashCode().ToString(CultureInfo.InvariantCulture)) &&
                            x.FileName.EndsWith(
                                dataset.EndYear.ToString(
                                    CultureInfo.InvariantCulture.DateTimeFormat.SortableDateTimePattern)));

                    if (lastestYear != null)
                    {
                        var stream = new MemoryStream();
                        lastestYear.Extract(stream);
                        stream.Position = 0;
                        var compressedValues = Serializer.Deserialize<YearlyDataBlock>(stream);
                        sensorobject.CurrentState.AddCompressedValues(compressedValues.CurrentValues);
                        sensorobject.RawData.AddCompressedValues(compressedValues.RawValues);
                    }

                    var secondToLastYear =
                        zip.Entries.FirstOrDefault(
                            x =>
                            x.FileName.Contains(sensorobject.Name.GetHashCode().ToString(CultureInfo.InvariantCulture)) &&
                            x.FileName.EndsWith(
                                dataset.EndYear.AddYears(-1).ToString(
                                    CultureInfo.InvariantCulture.DateTimeFormat.SortableDateTimePattern)));

                    if (secondToLastYear != null)
                    {
                        var stream = new MemoryStream();
                        secondToLastYear.Extract(stream);
                        stream.Position = 0;
                        var compressedValues = Serializer.Deserialize<YearlyDataBlock>(stream);
                        sensorobject.CurrentState.AddCompressedValues(compressedValues.CurrentValues);
                        sensorobject.RawData.AddCompressedValues(compressedValues.RawValues);
                    }

                    var numberOfYears = dataset.EndYear.Year - dataset.StartYear.Year;

                    dataset.HighestYearLoaded = numberOfYears;

                    if(numberOfYears > 0)
                    {
                        dataset.LowestYearLoaded = numberOfYears - 1;
                    }

                    dataset.Sensors.Add(sensorobject);
                }

                return dataset;
            }
        }

        public void SaveToFile(bool exportData = true, bool deleteFile = false)
        {
            if(deleteFile && File.Exists(SaveLocation))
                File.Delete(SaveLocation);

            if (!Directory.Exists(Common.DatasetSaveLocation))
                Directory.CreateDirectory(Common.DatasetSaveLocation);

            if (File.Exists(SaveLocation))
                File.Copy(SaveLocation, SaveLocation + ".backup", true);

            if (exportData)
                DatasetExporter.Export(this, Common.DatasetExportLocation(this), ExportFormat.CSV, true);

            /* .NET BinaryFormatter
            using (var stream = new FileStream(SaveLocation, FileMode.Create))
                new BinaryFormatter().Serialize(stream, this);
            
             * Protobuf
            using (var file = File.Create(SaveLocation))
                Serializer.Serialize(file, this);
            */

            using (var zip = File.Exists(SaveLocation) ? ZipFile.Read(SaveLocation) : new ZipFile())
            {
                zip.CompressionLevel = CompressionLevel.None;
                zip.Comment = string.Format("B3 ZIP FORMAT v1");

                var datasetStream = new MemoryStream();
                Serializer.Serialize(datasetStream, this);
                datasetStream.Position = 0;
                if (zip.EntryFileNames.Contains("dataset"))
                    zip.RemoveEntry("dataset");
                zip.AddEntry("dataset", datasetStream);

                var siteStream = new MemoryStream();
                Serializer.Serialize(siteStream, Site);
                siteStream.Position = 0;
                if (zip.EntryFileNames.Contains("site"))
                    zip.RemoveEntry("site");
                zip.AddEntry("site", siteStream);

                foreach (var sensor in Sensors)
                {
                    var sensorHash = sensor.Name.GetHashCode().ToString(CultureInfo.InvariantCulture);

                    var sensorMetaData = new MemoryStream();
                    Serializer.Serialize(sensorMetaData, sensor);
                    sensorMetaData.Position = 0;

                    if (!zip.EntryFileNames.Contains(sensorHash + "/"))
                        zip.AddDirectoryByName(sensorHash);
                    if (zip.EntryFileNames.Contains(sensorHash + "/metadata"))
                        zip.RemoveEntry(sensorHash + "\\metadata");
                    zip.AddEntry(sensorHash + "\\metadata", sensorMetaData);

                    for (var i = StartYear.AddYears(LowestYearLoaded); i < StartYear.AddYears(HighestYearLoaded + 1); i = i.AddYears(1))
                    {
                        var dataBlockStream = new MemoryStream();
                        var x = new YearlyDataBlock(sensor.CurrentState.GetCompressedValues(i, i.AddYears(1)),
                                                    sensor.RawData.GetCompressedValues(i, i.AddYears(1)));
                        Serializer.Serialize(dataBlockStream, x);
                        dataBlockStream.Position = 0;

                        if (zip.EntryFileNames.Contains(sensorHash + "/" + i.ToString(CultureInfo.InvariantCulture.DateTimeFormat.SortableDateTimePattern)))
                            zip.RemoveEntry(sensorHash + "\\" + i.ToString(CultureInfo.InvariantCulture.DateTimeFormat.SortableDateTimePattern));
                        zip.AddEntry(sensorHash + "\\" + i.ToString(CultureInfo.InvariantCulture.DateTimeFormat.SortableDateTimePattern), dataBlockStream);
                    }
                }

                zip.Save(SaveLocation);
            }
        }

        public void LoadInSensorData(int year, bool retainExistingValues = false)
        {
            LoadInSensorData(new[] { year }, retainExistingValues);
        }

        public void LoadInSensorData(int[] years, bool retainExistingValues = false)
        {
            using (var zip = ZipFile.Read(SaveLocation))
            {
                foreach (var year in years)
                {
                    foreach (var sensor in Sensors)
                    {
                        if (!retainExistingValues)
                            sensor.CurrentState.Values = new Dictionary<DateTime, float>();

                        var data =
                            zip.Entries.FirstOrDefault(
                                x =>
                                x.FileName.Contains(sensor.Name.GetHashCode().ToString(CultureInfo.InvariantCulture)) &&
                                x.FileName.EndsWith(
                                    StartYear.AddYears(year).ToString(
                                        CultureInfo.InvariantCulture.DateTimeFormat.SortableDateTimePattern)));

                        if (data != null)
                        {
                            var stream = new MemoryStream();
                            data.Extract(stream);
                            stream.Position = 0;
                            var compressedValues = Serializer.Deserialize<YearlyDataBlock>(stream);
                            sensor.CurrentState.AddCompressedValues(compressedValues.CurrentValues);
                            sensor.RawData.AddCompressedValues(compressedValues.RawValues);
                        }
                    }

                    if (retainExistingValues)
                    {
                        if (LowestYearLoaded > year)
                            LowestYearLoaded = year;
                        if (HighestYearLoaded < year)
                            HighestYearLoaded = year;
                    }
                    else
                    {
                        LowestYearLoaded = year;
                        HighestYearLoaded = year;
                    }
                }

            }


        }

        public void UnloadSensorData(int year, bool saveValues = true)
        {
            UnloadSensorData(new[] { year }, saveValues);
        }

        public void UnloadSensorData(int[] years, bool saveValues = true)
        {
            if (saveValues)
                SaveSensorData(years);

            foreach (var year in years)
            {
                foreach (var sensor in Sensors)
                {
                    var currentValuesToRemove =
                            sensor.CurrentState.Values.Where(
                                x =>
                                x.Key >= StartYear.AddYears(year) &&
                                x.Key < StartYear.AddYears(year + 1)).ToArray();
                    foreach (var keyValuePair in currentValuesToRemove)
                    {
                        sensor.CurrentState.Values.Remove(keyValuePair.Key);
                    }

                    var rawValuesToRemove =
                        sensor.RawData.Values.Where(
                            x =>
                            x.Key >= StartYear.AddYears(year) &&
                            x.Key < StartYear.AddYears(year + 1)).ToArray();
                    foreach (var keyValuePair in rawValuesToRemove)
                    {
                        sensor.RawData.Values.Remove(keyValuePair.Key);
                    }
                }
            }
            var timestamps = Sensors.SelectMany(x => x.CurrentState.Values).Select(x => x.Key).Distinct().ToArray();
            HighestYearLoaded = timestamps.Max().Year - StartYear.Year;
            LowestYearLoaded = timestamps.Min().Year - StartYear.Year;
        }

        public void SaveSensorData(int year)
        {
            SaveSensorData(new[] { year });
        }

        public void SaveSensorData(int[] years)
        {
            using (var zip = ZipFile.Read(SaveLocation))
            {
                zip.CompressionLevel = CompressionLevel.None;
                foreach (var year in years)
                {
                    foreach (var sensor in Sensors)
                    {
                        var sensorHash = sensor.Name.GetHashCode().ToString(CultureInfo.InvariantCulture);

                        var dataBlockStream = new MemoryStream();
                        var x = new YearlyDataBlock(sensor.CurrentState.GetCompressedValues(StartYear.AddYears(year), StartYear.AddYears(year + 1)),
                                                    sensor.RawData.GetCompressedValues(StartYear.AddYears(year), StartYear.AddYears(year + 1)));
                        Serializer.Serialize(dataBlockStream, x);
                        dataBlockStream.Position = 0;

                        if (zip.EntryFileNames.Contains(sensorHash + "/" + StartYear.AddYears(year).ToString(CultureInfo.InvariantCulture.DateTimeFormat.SortableDateTimePattern)))
                            zip.RemoveEntry(sensorHash + "\\" + StartYear.AddYears(year).ToString(CultureInfo.InvariantCulture.DateTimeFormat.SortableDateTimePattern));

                        zip.AddEntry(sensorHash + "\\" + StartYear.AddYears(year).ToString(CultureInfo.InvariantCulture.DateTimeFormat.SortableDateTimePattern), dataBlockStream);
                    }
                }
                zip.Save(SaveLocation);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}