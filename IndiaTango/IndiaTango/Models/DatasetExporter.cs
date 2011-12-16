using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace IndiaTango.Models
{
    public class DatasetExporter
    {
        public readonly Dataset Data;

        public DatasetExporter(Dataset data)
        {
            if (data == null)
                throw new ArgumentNullException("Dataset cannot be null");

            Data = data;
        }

        /// <summary>
        /// Exports a data set to a CSV file.
        /// The file is saved in the same format as the original CSV files.
        /// </summary>
        /// <param name="filePath">The desired path and file name of the file to be saved. No not include an extension.</param>
        /// <param name="format">The format to save the file in.</param>
        /// <param name="includeEmptyLines">Wether to export the file with empty lines or not.</param>
        public void Export(string filePath, ExportFormat format, bool includeEmptyLines)
        {
            Export(filePath, format, includeEmptyLines, false, false, ExportedPoints.AllPoints, DateColumnFormat.TwoDateColumn);
        }

        /// <summary>
        /// Exports a data set to a CSV file.
        /// The file is saved in the same format as the original CSV files.
        /// </summary>
        /// <param name="filePath">The desired path and file name of the file to be saved. No not include an extension.</param>
        /// <param name="format">The format to save the file in.</param>
        /// <param name="includeEmptyLines">Wether to export the file with empty lines or not.</param>
        /// <param name="addMetaDataFile">Wether to export the file with embedded site meta data.</param>
        /// <param name="includeChangeLog">Wether to include a seperate log file that details the changes made to the data.</param>
        public void Export(string filePath, ExportFormat format, bool includeEmptyLines, bool addMetaDataFile, bool includeChangeLog)
        {
            Export(filePath, format, includeEmptyLines, addMetaDataFile, includeChangeLog, ExportedPoints.AllPoints, DateColumnFormat.TwoDateColumn, false);
        }

        /// <summary>
        /// Exports a data set to a CSV file.
        /// The file is saved in the same format as the original CSV files.
        /// </summary>
        /// <param name="filePath">The desired path and file name of the file to be saved. No not include an extension.</param>
        /// <param name="format">The format to save the file in.</param>
        /// <param name="includeEmptyLines">Wether to export the file with empty lines or not.</param>
        /// <param name="addMetaDataFile">Wether to export the file with embedded site meta data.</param>
        /// <param name="includeChangeLog">Wether to include a seperate log file that details the changes made to the data.</param>
        /// <param name="exportedPoints">What points to export.</param>
        /// <param name="dateColumnFormat">Wether to split the two date/time columns into five seperate columns</param>
        public void Export(string filePath, ExportFormat format, bool includeEmptyLines, bool addMetaDataFile, bool includeChangeLog, ExportedPoints exportedPoints, DateColumnFormat dateColumnFormat)
        {
            Export(filePath, format, includeEmptyLines, addMetaDataFile, includeChangeLog, exportedPoints, dateColumnFormat, false);
        }

        /// <summary>
        /// Exports a data set to a CSV file.
        /// The file is saved in the same format as the original CSV files.
        /// </summary>
        /// <param name="filePath">The desired path and file name of the file to be saved. No not include an extension.</param>
        /// <param name="format">The format to save the file in.</param>
        /// <param name="includeEmptyLines">Wether to export the file with empty lines or not.</param>
        /// <param name="addMetaDataFile">Wether to export the file with embedded site meta data.</param>
        /// <param name="includeChangeLog">Wether to include a seperate log file that details the changes made to the data.</param>
        /// <param name="exportedPoints">What points to export.</param>
        /// <param name="dateColumnFormat">Wether to split the two date/time columns into five seperate columns</param>
        /// <param name="exportRaw">Whether to export the raw data or the current state.</param>
        public void Export(string filePath, ExportFormat format, bool includeEmptyLines, bool addMetaDataFile, bool includeChangeLog, ExportedPoints exportedPoints, DateColumnFormat dateColumnFormat, bool exportRaw)
        {
            EventLogger.LogInfo(Data, GetType().ToString(), "Data export started.");

            if (String.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException("filePath cannot be null");

            if (format == null)
                throw new ArgumentNullException("Export format cannot be null");

            //Strip the existing extension and add the one specified in the method args
            filePath = Path.ChangeExtension(filePath, format.Extension);
            string metaDataFilePath = filePath + " Site Meta Data.txt";
            string changeMatrixFilePath = filePath + " Changes Matrix.csv";
            var changesFilePath = filePath + " Changes Log.txt";
            var numOfPointsToSummarise = 1;

            if (exportedPoints.NumberOfMinutes != 0)
                numOfPointsToSummarise = exportedPoints.NumberOfMinutes / 15;

            if (format.Equals(ExportFormat.CSV))
            {
                ExportCSV(filePath, includeEmptyLines, dateColumnFormat, false, numOfPointsToSummarise);

                if (exportRaw)
                    ExportCSV(filePath + " Raw.csv", includeEmptyLines, dateColumnFormat, true, numOfPointsToSummarise);

                if (addMetaDataFile && Data.Site != null)
                    ExportMetaData(filePath, metaDataFilePath);

                if (includeChangeLog)
                    ExportChangesFile(filePath, changeMatrixFilePath, changesFilePath, dateColumnFormat);

                EventLogger.LogInfo(Data, GetType().ToString(), "Data export complete. File saved to: " + filePath);
            }
            else if (format.Equals(ExportFormat.XLSX))
            {
                throw new NotImplementedException("Cannot export as XLSX yet.");
            }
            else
            {
                throw new NotImplementedException("File format not supported.");
            }
        }

        private void ExportCSV(string filePath, bool includeEmptyLines, DateColumnFormat dateColumnFormat, bool exportRaw, int numOfPointsToSummarise)
        {
            using (StreamWriter writer = File.CreateText(filePath))
            {
                const char del = ',';
                var columnHeadings = dateColumnFormat.Equals(DateColumnFormat.SplitDateColumn)
                                            ? "DD" + del + "MM" + del + "YYYY" + del + "hh" + del + "mm"
                                            : "DD/MM/YYYY" + del + "hhmm";
                var currentSensorIndex = 0;
                var outputData = new string[Data.Sensors.Count, (Data.ExpectedDataPointCount / numOfPointsToSummarise) + 1];
                var rowDate = Data.StartTimeStamp;


                foreach (var sensor in Data.Sensors)
                {
                    var stateToUse = (exportRaw) ? sensor.RawData : sensor.CurrentState;

                    //Construct the column headings (Sensor names)
                    columnHeadings += del + sensor.Name;
                    var i = Data.StartTimeStamp;
                    while (i <= Data.EndTimeStamp)
                    {
                        var sum = float.MinValue;
                        for (var j = 0; j < numOfPointsToSummarise; j++, i = i.AddMinutes(15))
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
                                    GetArrayRowFromTime(Data.StartTimeStamp, i.AddMinutes((-15) * numOfPointsToSummarise),
                                                        numOfPointsToSummarise)] =
                                    Math.Round((sum / numOfPointsToSummarise), 2).ToString();
                            else
                                outputData[
                                    currentSensorIndex,
                                    GetArrayRowFromTime(Data.StartTimeStamp, i.AddMinutes((-15) * numOfPointsToSummarise),
                                                        numOfPointsToSummarise)] =
                                    Math.Round((sum), 2).ToString();
                        }
                    }
                    currentSensorIndex++;
                }

                //Strip the last delimiter from the headings and write the line
                writer.WriteLine(columnHeadings);

                //write the data here...
                for (int row = 0; row < Data.ExpectedDataPointCount / numOfPointsToSummarise; row++)
                {
                    string line = "";

                    for (int col = 0; col < Data.Sensors.Count; col++)
                        line += del + outputData[col, row];

                    if (includeEmptyLines || line.Length != Data.Sensors.Count)
                    {
                        line = dateColumnFormat.Equals(DateColumnFormat.SplitDateColumn)
                                ? rowDate.ToString("dd") + del + rowDate.ToString("MM") + del + rowDate.ToString("yyyy") + del +
                                  rowDate.ToString("HH") + del + rowDate.ToString("mm") + line
                                : rowDate.ToString("dd/MM/yyyy") + del + rowDate.ToString("HH:mm") + line;
                        writer.WriteLine(line);
                    }

                    rowDate = rowDate.AddMinutes(15 * numOfPointsToSummarise);
                }

                writer.Close();
            }
        }

        private void ExportChangesFile(string filePath, string changeMatrixFilePath, string changesFilePath, DateColumnFormat dateColumnFormat)
        {
            var changesUsed = new List<int>();
            using (var writer = File.CreateText(changeMatrixFilePath))
            {
                writer.WriteLine("Change matrix for file: " + Path.GetFileName(filePath));
                var line = dateColumnFormat.Equals(DateColumnFormat.SplitDateColumn)
                               ? "Day,Month,Year,Hours,Minutes" + ','
                               : "Date,Time" + ',';
                line = Data.Sensors.Aggregate(line, (current, sensor) => current + (sensor.Name + ","));
                line = line.Remove(line.Count() - 2);
                writer.WriteLine(line);
                for (var time = Data.StartTimeStamp; time <= Data.EndTimeStamp; time = time.AddMinutes(Data.DataInterval))
                {
                    line = dateColumnFormat.Equals(DateColumnFormat.SplitDateColumn)
                            ? time.ToString("dd") + ',' + time.ToString("MM") + ',' + time.ToString("yyyy") + ',' +
                              time.ToString("HH") + ',' + time.ToString("mm") + ','
                            : time.ToString("dd/MM/yyyy") + ',' + time.ToString("HH:mm") + ',';
                    foreach (var sensor in Data.Sensors)
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

        private void ExportMetaData(string filePath, string metaDataFilePath)
        {
            using (StreamWriter writer = File.CreateText(metaDataFilePath))
            {
                writer.WriteLine("Site details for file: " + Path.GetFileName(filePath));
                writer.WriteLine("ID: " + Data.Site.Id);
                writer.WriteLine("Name: " + Data.Site.Name);
                writer.WriteLine("Owner: " + Data.Site.Owner);

                //TODO: clean up
                if (Data.Site.PrimaryContact != null)
                {
                    writer.WriteLine("Primary Contact:");
                    writer.WriteLine("\tName: " + Data.Site.PrimaryContact.FirstName + " " +
                                     Data.Site.PrimaryContact.LastName);
                    writer.WriteLine("\tBusiness: " + Data.Site.PrimaryContact.Business);
                    writer.WriteLine("\tPhone: " + Data.Site.PrimaryContact.Phone);
                    writer.WriteLine("\tEmail: " + Data.Site.PrimaryContact.Email);
                }

                if (Data.Site.SecondaryContact != null)
                {
                    writer.WriteLine("Secondary Contact:");
                    writer.WriteLine("\tName: " + Data.Site.SecondaryContact.FirstName + " " +
                                     Data.Site.SecondaryContact.LastName);
                    writer.WriteLine("\tBusiness: " + Data.Site.SecondaryContact.Business);
                    writer.WriteLine("\tPhone: " + Data.Site.SecondaryContact.Phone);
                    writer.WriteLine("\tEmail: " + Data.Site.SecondaryContact.Email);
                }

                if (Data.Sensors != null && Data.Sensors.Count > 0)
                {
                    writer.WriteLine("Series:");
                    foreach (var sensor in Data.Sensors)
                    {
                        writer.WriteLine("\t" + sensor.Name);
                        writer.WriteLine("\t\tDescription: " + sensor.Description);
                        writer.WriteLine("\t\tSensor Type: " + sensor.SensorType);
                        writer.WriteLine("\t\tUnit: " + sensor.Unit);
                        writer.WriteLine("\t\tDepth (m): " + sensor.Depth);
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

        private int GetArrayRowFromTime(DateTime startDate, DateTime currentDate, int numOfPointsToAverage)
        {
            if (currentDate < startDate)
                throw new ArgumentException("currentDate must be larger than or equal to startDate\nYou supplied startDate=" + startDate.ToString() + " currentDate=" + currentDate.ToString());

            return (int)Math.Floor(currentDate.Subtract(startDate).TotalMinutes / Data.DataInterval / numOfPointsToAverage);
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

        public static DateColumnFormat TwoDateColumn { get { return new DateColumnFormat("Two Column", "Two date and time columns (DD/MM/YYYY | hhmm)"); } }

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