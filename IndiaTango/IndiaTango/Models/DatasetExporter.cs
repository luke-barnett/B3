using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace IndiaTango.Models
{
    /// <summary>
    /// Exports a dataset
    /// </summary>
    public static class DatasetExporter
    {
        /// <summary>
        /// Exports a data set to a CSV file.
        /// The file is saved in the same format as the original CSV files.
        /// </summary>
        /// <param name="data">The dataset to export</param>
        /// <param name="filePath">The desired path and file name of the file to be saved. No not include an extension.</param>
        /// <param name="format">The format to save the file in.</param>
        /// <param name="includeEmptyLines">Wether to export the file with empty lines or not.</param>
        /// <param name="loadInUnloadedValues">Whether or not to load in any unloaded values</param>
        public static void Export(Dataset data, string filePath, ExportFormat format, bool includeEmptyLines, bool loadInUnloadedValues = true)
        {
            Export(data, filePath, format, includeEmptyLines, false, false, ExportedPoints.AllPoints, DateColumnFormat.TwoDateColumn, false, loadInUnloadedValues);
        }

        /// <summary>
        /// Exports a data set to a CSV file.
        /// The file is saved in the same format as the original CSV files.
        /// </summary>
        /// <param name="data">The dataset to export</param>
        /// <param name="filePath">The desired path and file name of the file to be saved. No not include an extension.</param>
        /// <param name="format">The format to save the file in.</param>
        /// <param name="includeEmptyLines">Wether to export the file with empty lines or not.</param>
        /// <param name="addMetaDataFile">Wether to export the file with embedded site meta data.</param>
        /// <param name="includeChangeLog">Wether to include a seperate log file that details the changes made to the data.</param>
        /// <param name="loadInUnloadedValues">Whether or not to load in any unloaded values</param>
        public static void Export(Dataset data, string filePath, ExportFormat format, bool includeEmptyLines, bool addMetaDataFile, bool includeChangeLog, bool loadInUnloadedValues = true)
        {
            Export(data, filePath, format, includeEmptyLines, addMetaDataFile, includeChangeLog, ExportedPoints.AllPoints, DateColumnFormat.TwoDateColumn, false, loadInUnloadedValues);
        }

        /// <summary>
        /// Exports a data set to a CSV file.
        /// The file is saved in the same format as the original CSV files.
        /// </summary>
        /// <param name="data">The dataset to export</param>
        /// <param name="filePath">The desired path and file name of the file to be saved. No not include an extension.</param>
        /// <param name="format">The format to save the file in.</param>
        /// <param name="includeEmptyLines">Wether to export the file with empty lines or not.</param>
        /// <param name="addMetaDataFile">Wether to export the file with embedded site meta data.</param>
        /// <param name="includeChangeLog">Wether to include a seperate log file that details the changes made to the data.</param>
        /// <param name="exportedPoints">What points to export.</param>
        /// <param name="dateColumnFormat">Wether to split the two date/time columns into five seperate columns</param>
        /// <param name="loadInUnloadedValues">Whether or not to load in any unloaded values</param>
        public static void Export(Dataset data, string filePath, ExportFormat format, bool includeEmptyLines, bool addMetaDataFile, bool includeChangeLog, ExportedPoints exportedPoints, DateColumnFormat dateColumnFormat, bool loadInUnloadedValues = true)
        {
            Export(data, filePath, format, includeEmptyLines, addMetaDataFile, includeChangeLog, exportedPoints, dateColumnFormat, false, loadInUnloadedValues);
        }

        /// <summary>
        /// Exports a data set to a CSV file.
        /// The file is saved in the same format as the original CSV files.
        /// </summary>
        /// <param name="data">The dataset to export</param>
        /// <param name="filePath">The desired path and file name of the file to be saved. No not include an extension.</param>
        /// <param name="format">The format to save the file in.</param>
        /// <param name="includeEmptyLines">Wether to export the file with empty lines or not.</param>
        /// <param name="addMetaDataFile">Wether to export the file with embedded site meta data.</param>
        /// <param name="includeChangeLog">Wether to include a seperate log file that details the changes made to the data.</param>
        /// <param name="exportedPoints">What points to export.</param>
        /// <param name="dateColumnFormat">Wether to split the two date/time columns into five seperate columns</param>
        /// <param name="exportRaw">Whether to export the raw data or the current state.</param>
        /// <param name="loadInUnloadedValues">Whether or not to load in any unloaded values</param>
        /// <param name="copyLogFiles">Whether or not to copy the log files</param>
        public static void Export(Dataset data, string filePath, ExportFormat format, bool includeEmptyLines, bool addMetaDataFile, bool includeChangeLog, ExportedPoints exportedPoints, DateColumnFormat dateColumnFormat, bool exportRaw, bool loadInUnloadedValues, bool copyLogFiles = false)
        {
            if (data == null)
                throw new ArgumentNullException("Dataset cannot be null");

            //LOAD IN ALL THE VALUES
            var firstYearLoaded = data.LowestYearLoaded;
            var lastYearLoaded = data.HighestYearLoaded;

            if (loadInUnloadedValues)
            {
                if (firstYearLoaded != 0)
                {
                    for (var i = 0; i < firstYearLoaded; i++)
                    {
                        data.LoadInSensorData(i, true);
                    }
                }

                if (data.EndYear > data.StartYear.AddYears(lastYearLoaded + 1))
                {
                    for (var i = lastYearLoaded + 1; data.EndYear >= data.StartYear.AddYears(i + 1); i++)
                    {
                        data.LoadInSensorData(i, true);
                    }
                }
            }


            EventLogger.LogInfo(data, "EXPORTER", "Data export started.");

            if (String.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException("filePath cannot be null");

            if (format == null)
                throw new ArgumentNullException("Export format cannot be null");

            //Strip the existing extension and add the one specified in the method args
            filePath = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath),
                                    Path.GetFileName(filePath));
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            else
            {
                throw new Exception("Cannot Overwrite Existing Data");
            }
            filePath = Path.ChangeExtension(filePath, format.Extension);
            string metaDataFilePath = filePath + " Site Meta Data.txt";
            string changeMatrixFilePath = filePath + " Changes Matrix.csv";
            var changesFilePath = filePath + " Changes Log.txt";
            var numOfPointsToSummarise = 1;

            if (exportedPoints.NumberOfMinutes != 0)
                numOfPointsToSummarise = exportedPoints.NumberOfMinutes / data.DataInterval;

            if (format.Equals(ExportFormat.CSV))
            {
                ExportCSV(data, filePath, includeEmptyLines, dateColumnFormat, false, numOfPointsToSummarise);

                if (exportRaw)
                    ExportCSV(data, filePath + " Raw.csv", includeEmptyLines, dateColumnFormat, true, numOfPointsToSummarise);

                if (addMetaDataFile && data.Site != null)
                    ExportMetaData(data, filePath, metaDataFilePath);

                if (includeChangeLog)
                    ExportChangesFile(data, filePath, changeMatrixFilePath, changesFilePath, dateColumnFormat);

                EventLogger.LogInfo(data, "EXPORTER", "Data export complete. File saved to: " + filePath);
            }
            else if (format.Equals(ExportFormat.XLSX))
            {
                throw new NotImplementedException("Cannot export as XLSX yet.");
            }
            else
            {
                throw new NotImplementedException("File format not supported.");
            }

            if (copyLogFiles && data.Site != null && Directory.Exists(Path.Combine(Common.AppDataPath, "Logs", data.Site.Name, "SensorLogs")))
            {
                var sourcePath = Path.Combine(Common.AppDataPath, "Logs", data.Site.Name, "SensorLogs");
                var destinationPath = Path.Combine(Path.GetDirectoryName(filePath), data.Site.Name + " Sensor Logs");

                if (!Directory.Exists(destinationPath))
                    Directory.CreateDirectory(destinationPath);

                //Now Create all of the directories
                foreach (string dirPath in Directory.GetDirectories(sourcePath, "*",
                    SearchOption.AllDirectories))
                    Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));

                //Copy all the files
                foreach (string newPath in Directory.GetFiles(sourcePath, "*.*",
                    SearchOption.AllDirectories))
                    File.Copy(newPath, newPath.Replace(sourcePath, destinationPath));
            }

            if (loadInUnloadedValues)
            {
                //Unload all values not in our time range
                foreach (var sensor in data.Sensors)
                {
                    var currentValuesToRemove =
                        sensor.CurrentState.Values.Where(
                            x =>
                            x.Key < data.StartYear.AddYears(firstYearLoaded) ||
                            x.Key >= data.StartYear.AddYears(lastYearLoaded + 1)).ToArray();
                    foreach (var keyValuePair in currentValuesToRemove)
                    {
                        sensor.CurrentState.Values.Remove(keyValuePair.Key);
                    }

                    var rawValuesToRemove =
                        sensor.RawData.Values.Where(
                            x =>
                            x.Key < data.StartYear.AddYears(firstYearLoaded) ||
                            x.Key >= data.StartYear.AddYears(lastYearLoaded + 1)).ToArray();
                    foreach (var keyValuePair in rawValuesToRemove)
                    {
                        sensor.RawData.Values.Remove(keyValuePair.Key);
                    }
                }

                data.LowestYearLoaded = firstYearLoaded;
                data.HighestYearLoaded = lastYearLoaded;
            }
        }

        /// <summary>
        /// Exports as a CSV
        /// </summary>
        /// <param name="data"></param>
        /// <param name="filePath"></param>
        /// <param name="includeEmptyLines"></param>
        /// <param name="dateColumnFormat"></param>
        /// <param name="exportRaw"></param>
        /// <param name="numOfPointsToSummarise"></param>
        private static void ExportCSV(Dataset data, string filePath, bool includeEmptyLines, DateColumnFormat dateColumnFormat, bool exportRaw, int numOfPointsToSummarise)
        {
            using (StreamWriter writer = File.CreateText(filePath))
            {
                const char del = ',';
                var columnHeadings = dateColumnFormat.Equals(DateColumnFormat.SplitDateColumn)
                                            ? "DD" + del + "MM" + del + "YYYY" + del + "hh" + del + "mm"
                                            : "DD/MM/YYYY" + del + "hh:mm";
                var currentSensorIndex = 0;
                var outputData = new string[data.Sensors.Count, (data.ExpectedDataPointCount / numOfPointsToSummarise) + 1];
                var rowDate = data.StartTimeStamp;


                foreach (var sensor in data.Sensors.OrderBy(x => x.SortIndex))
                {
                    var stateToUse = (exportRaw) ? sensor.RawData : sensor.CurrentState;

                    //Construct the column headings (Sensor names)
                    columnHeadings += del + sensor.Name;
                    var i = data.StartTimeStamp;
                    while (i <= data.EndTimeStamp)
                    {
                        var sum = float.MinValue;
                        for (var j = 0; j < numOfPointsToSummarise; j++, i = i.AddMinutes(data.DataInterval))
                        {
                            float value;
                            if (stateToUse.Values.TryGetValue(i, out value))
                                if (sum.Equals(float.MinValue))
                                    sum = value;
                                else
                                    sum += value;
                        }

                        if (!sum.Equals(float.MinValue))
                        {
                            if (sensor.SummaryType == SummaryType.Average)
                                outputData[
                                    currentSensorIndex,
                                    GetArrayRowFromTime(data, data.StartTimeStamp, i.AddMinutes((-data.DataInterval) * numOfPointsToSummarise),
                                                        numOfPointsToSummarise)] =
                                    Math.Round((sum / numOfPointsToSummarise), 2).ToString();
                            else
                                outputData[
                                    currentSensorIndex,
                                    GetArrayRowFromTime(data, data.StartTimeStamp, i.AddMinutes((-data.DataInterval) * numOfPointsToSummarise),
                                                        numOfPointsToSummarise)] =
                                    Math.Round((sum), 2).ToString();
                        }
                    }
                    currentSensorIndex++;
                }

                //Strip the last delimiter from the headings and write the line
                writer.WriteLine(columnHeadings);

                //write the data here...
                for (int row = 0; row < data.ExpectedDataPointCount / numOfPointsToSummarise; row++)
                {
                    string line = "";

                    for (int col = 0; col < data.Sensors.Count; col++)
                        line += del + outputData[col, row];

                    if (includeEmptyLines || line.Length != data.Sensors.Count)
                    {
                        line = dateColumnFormat.Equals(DateColumnFormat.SplitDateColumn)
                                ? rowDate.ToString("dd") + del + rowDate.ToString("MM") + del + rowDate.ToString("yyyy") + del +
                                  rowDate.ToString("HH") + del + rowDate.ToString("mm") + line
                                : rowDate.ToString("dd/MM/yyyy") + del + rowDate.ToString("HH:mm") + line;
                        writer.WriteLine(line);
                    }

                    rowDate = rowDate.AddMinutes(data.DataInterval * numOfPointsToSummarise);
                }

                writer.Close();
            }
        }

        /// <summary>
        /// Exports the changes file
        /// </summary>
        /// <param name="data"></param>
        /// <param name="filePath"></param>
        /// <param name="changeMatrixFilePath"></param>
        /// <param name="changesFilePath"></param>
        /// <param name="dateColumnFormat"></param>
        private static void ExportChangesFile(Dataset data, string filePath, string changeMatrixFilePath, string changesFilePath, DateColumnFormat dateColumnFormat)
        {
            var changesUsed = new List<int>();
            using (var writer = File.CreateText(changeMatrixFilePath))
            {
                writer.WriteLine("Change matrix for file: " + Path.GetFileName(filePath));
                var line = dateColumnFormat.Equals(DateColumnFormat.SplitDateColumn)
                               ? "Day,Month,Year,Hours,Minutes" + ','
                               : "Date,Time" + ',';
                line = data.Sensors.OrderBy(x => x.SortIndex).Aggregate(line, (current, sensor) => current + (sensor.Name + ","));
                line = line.Remove(line.Count() - 2);
                writer.WriteLine(line);
                for (var time = data.StartTimeStamp; time <= data.EndTimeStamp; time = time.AddMinutes(data.DataInterval))
                {
                    line = dateColumnFormat.Equals(DateColumnFormat.SplitDateColumn)
                            ? time.ToString("dd") + ',' + time.ToString("MM") + ',' + time.ToString("yyyy") + ',' +
                              time.ToString("HH") + ',' + time.ToString("mm") + ','
                            : time.ToString("dd/MM/yyyy") + ',' + time.ToString("HH:mm") + ',';
                    foreach (var sensor in data.Sensors.OrderBy(x => x.SortIndex))
                    {
                        LinkedList<int> vals;
                        if (sensor.CurrentState.Changes.TryGetValue(time, out vals))
                        {
                            changesUsed.AddRange(vals.Where(x => !changesUsed.Contains(x)));
                            line = vals.Aggregate(line, (current, val) => current + (val + " "));
                        }
                        line += ",";
                    }
                    line = line.Remove(line.Count() - 2);
                    writer.WriteLine(line);
                }
            }
            using (var writer = File.CreateText(changesFilePath))
            {
                writer.WriteLine("Change log for file " + Path.GetFileName(filePath));
                foreach (var i in changesUsed.OrderBy(i => i))
                {
                    Debug.Print("Change number " + i);
                    writer.WriteLine(i == -1
                                         ? new ChangeReason(-1, "Reason not specified")
                                         : ChangeReason.ChangeReasons.FirstOrDefault(x => x.ID == i));
                }
            }
        }

        /// <summary>
        /// Exports the metadata
        /// </summary>
        /// <param name="data"></param>
        /// <param name="filePath"></param>
        /// <param name="metaDataFilePath"></param>
        private static void ExportMetaData(Dataset data, string filePath, string metaDataFilePath)
        {
            using (StreamWriter writer = File.CreateText(metaDataFilePath))
            {
                writer.WriteLine("Site details for file: " + Path.GetFileName(filePath));
                writer.WriteLine("ID: " + data.Site.Id);
                writer.WriteLine("Name: " + data.Site.Name);
                writer.WriteLine("Owner: " + data.Site.Owner);
                writer.WriteLine("Site notes:\r\n\t" + data.Site.SiteNotes);
                writer.WriteLine("Editing notes:" + data.Site.EditingNotes);

                if (data.Site.PrimaryContact != null)
                {
                    writer.WriteLine("Primary Contact:");
                    writer.WriteLine("\tName: " + data.Site.PrimaryContact.FirstName + " " +
                                     data.Site.PrimaryContact.LastName);
                    writer.WriteLine("\tBusiness: " + data.Site.PrimaryContact.Business);
                    writer.WriteLine("\tPhone: " + data.Site.PrimaryContact.Phone);
                    writer.WriteLine("\tEmail: " + data.Site.PrimaryContact.Email);
                }

                if (data.Site.SecondaryContact != null)
                {
                    writer.WriteLine("Secondary Contact:");
                    writer.WriteLine("\tName: " + data.Site.SecondaryContact.FirstName + " " +
                                     data.Site.SecondaryContact.LastName);
                    writer.WriteLine("\tBusiness: " + data.Site.SecondaryContact.Business);
                    writer.WriteLine("\tPhone: " + data.Site.SecondaryContact.Phone);
                    writer.WriteLine("\tEmail: " + data.Site.SecondaryContact.Email);
                }

                if (data.Sensors != null && data.Sensors.Count > 0)
                {
                    writer.WriteLine("Series:");
                    foreach (var sensor in data.Sensors.OrderBy(x => x.SortIndex))
                    {
                        writer.WriteLine("\t" + sensor.Name);
                        writer.WriteLine("\t\tDescription: " + sensor.Description);
                        writer.WriteLine("\t\tSensor Type: " + sensor.SensorType);
                        writer.WriteLine("\t\tUnit: " + sensor.Unit);
                        writer.WriteLine("\t\tDepth (m): " + sensor.Elevation);
                        writer.WriteLine("\t\tSensors:");
                        foreach (var metaData in sensor.MetaData)
                        {
                            writer.WriteLine("\t\t\tSerial Number: " + metaData.SerialNumber);
                            writer.WriteLine("\t\t\tManufacturer: " + metaData.Manufacturer);
                            writer.WriteLine("\t\t\tAccuracy: " + metaData.Accuracy);
                            writer.WriteLine("\t\t\tDate of Installation: " + metaData.DateOfInstallation);
                            writer.WriteLine("\t\tIdeal Calibration Frequency (Days): " + metaData.IdealCalibrationFrequency.Days);
                        }
                        writer.WriteLine("\t\tCalibrations:");
                        foreach (var calibration in sensor.Calibrations)
                        {
                            writer.WriteLine("\t\t\t" + calibration);
                        }

                    }
                }

                Debug.WriteLine(metaDataFilePath);
                writer.Close();
            }
        }

        private static int GetArrayRowFromTime(Dataset data, DateTime startDate, DateTime currentDate, int numOfPointsToAverage)
        {
            if (currentDate < startDate)
                throw new ArgumentException("currentDate must be larger than or equal to startDate\nYou supplied startDate=" + startDate.ToString() + " currentDate=" + currentDate.ToString());

            return (int)Math.Floor(currentDate.Subtract(startDate).TotalMinutes / data.DataInterval / numOfPointsToAverage);
        }
    }

    public class ExportFormat
    {
        readonly string _extension;
        readonly string _name;

        #region PrivateConstructor

        private ExportFormat(string extension, string name)
        {
            _extension = extension;
            _name = name;
        }

        #endregion

        #region PublicProperties

        public string Extension { get { return _extension; } }

        public string Name { get { return _name; } }

        public string FilterText { get { return ToString() + "|*" + _extension; } }

        public static ExportFormat CSV { get { return new ExportFormat(".csv", "Comma Seperated Value File"); } }

        public static ExportFormat TXT { get { return new ExportFormat(".txt", "Tab Deliminated Text File"); } }

        public static ExportFormat XLSX { get { return new ExportFormat(".xlsx", "Excel Workbook"); } }

        #endregion

        #region PublicMethods

        public new string ToString()
        {
            return Name + "(*" + Extension + ")";
        }

        public override bool Equals(object obj)
        {
            return (obj is ExportFormat) && (obj as ExportFormat).Extension.CompareTo(Extension) == 0 &&
                   (obj as ExportFormat).Name.CompareTo(Name) == 0;
        }

        #endregion

    }

    public class DateColumnFormat
    {
        readonly string _description;
        readonly string _name;

        #region PrivateConstructor

        private DateColumnFormat(string name, string description)
        {
            _description = description;
            _name = name;
        }

        #endregion

        #region PublicProperties

        public string Description { get { return _description; } }

        public string Name { get { return _name; } }

        public static DateColumnFormat TwoDateColumn { get { return new DateColumnFormat("Two Column", "Two date and time columns (DD/MM/YYYY | hh:mm)"); } }

        public static DateColumnFormat SplitDateColumn { get { return new DateColumnFormat("Split Column", "Split date and time columns (DD | MM | YYYY | hh | mm)"); } }

        #endregion

        #region PublicMethods

        public override string ToString()
        {
            return _description;
        }

        public override bool Equals(object obj)
        {
            return (obj is DateColumnFormat) && (obj as DateColumnFormat).Description.CompareTo(Description) == 0 &&
                   (obj as DateColumnFormat).Name.CompareTo(Name) == 0;
        }

        #endregion

    }

    public class ExportedPoints
    {
        readonly string _description;
        readonly string _name;

        #region PrivateConstructor

        private ExportedPoints(string name, string description, int mins)
        {
            _description = description;
            _name = name;
            NumberOfMinutes = mins;
        }

        #endregion

        #region PublicProperties

        public string Description { get { return _description; } }

        public string Name { get { return _name; } }

        public static ExportedPoints AllPoints { get { return new ExportedPoints("All Points", "All data points", 0); } }

        public static ExportedPoints HourlyPoints { get { return new ExportedPoints("Hourly Points", "Hourly readings", 60); } }

        public static ExportedPoints DailyPoints { get { return new ExportedPoints("Daily Points", "Daily readings", 60 * 24); } }

        public static ExportedPoints WeeklyPoints { get { return new ExportedPoints("Weekly Points", "Weekly readings", 60 * 24 * 7); } }

        public int NumberOfMinutes { get; private set; }

        #endregion

        #region PublicMethods

        public override string ToString()
        {
            return _description;
        }

        public override bool Equals(object obj)
        {
            return (obj is ExportedPoints) && (obj as ExportedPoints).Description.CompareTo(Description) == 0 &&
                   (obj as ExportedPoints).Name.CompareTo(Name) == 0;
        }

        #endregion

    }
}