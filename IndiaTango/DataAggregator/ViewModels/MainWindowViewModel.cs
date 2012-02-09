using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Caliburn.Micro;
using DataAggregator.Models;
using Microsoft.Win32;

namespace DataAggregator.ViewModels
{
    public enum DateTimeComponentType
    {
        YYYY,
        MM,
        DD,
        // ReSharper disable InconsistentNaming
        hh,
        mm,
        hhmm,
        // ReSharper restore InconsistentNaming
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
    }

    public enum GroupingType
    {
        Centered,
        Forward
    }

    public class MainWindowViewModel : Screen
    {
        private List<Series> _series;
        private List<DateTime> _timestamps;
        private TimeSpanOption _aggregationTimeSpan = AggregationModel.TimeSpanOptions[0];
        private bool _lockedIn;
        private Cursor _applicationCursor = Cursors.Arrow;
        private int _progress;
        private GroupingType _groupingType = GroupingType.Centered;

        public MainWindowViewModel()
        {
            _timestamps = new List<DateTime>();
        }

        public List<Series> Series
        {
            get { return _series; }
            set
            {
                _series = value;
                NotifyOfPropertyChange(() => Series);
            }
        }

        public TimeSpanOption AggregationTimeSpan
        {
            get { return _aggregationTimeSpan; }
            set
            {
                _aggregationTimeSpan = value;
                NotifyOfPropertyChange(() => AggregationTimeSpan);
            }
        }

        public TimeSpanOption[] TimeSpanOptions
        {
            get { return AggregationModel.TimeSpanOptions; }
        }

        public bool LockedIn
        {
            get { return _lockedIn; }
            set
            {
                _lockedIn = value;
                NotifyOfPropertyChange(() => LockedIn);
                NotifyOfPropertyChange(() => ActionsEnabled);
            }
        }

        public bool ActionsEnabled
        {
            get
            {
                {
                    return !_lockedIn;
                }

            }
        }

        public Cursor ApplicationCursor
        {
            get { return _applicationCursor; }
            set
            {
                _applicationCursor = value;
                NotifyOfPropertyChange(() => ApplicationCursor);
            }
        }

        public string Title
        {
            get { return "Data Aggregator"; }
        }

        public int Progress
        {
            get { return _progress; }
            set
            {
                _progress = value;
                NotifyOfPropertyChange(() => Progress);
                NotifyOfPropertyChange(() => ProgressText);
            }
        }

        public string ProgressText
        {
            get { return string.Format("{0}%", Progress); }
        }

        public void Load()
        {
            var openFileDialog = new OpenFileDialog { Filter = @"CSV Files|*.csv" };
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    LoadCSV(openFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Failed to load CSV file\r\nException\r\n{0}", ex), "Failed to import",
                                    MessageBoxButton.OK);
                }
            }
        }

        public void Export()
        {
            if (Series.Count == 0)
            {
                MessageBox.Show("You need to import values to export");
                return;
            }

            var saveFileDialog = new SaveFileDialog { Filter = @"CSV Files|*.csv" };
            if (saveFileDialog.ShowDialog() == true)
            {
                var bw = new BackgroundWorker();
                bw.DoWork += (o, e) =>
                MessageBox.Show(Exporter.Export(_timestamps.ToArray(), Series.ToArray(), AggregationTimeSpan, saveFileDialog.FileName, bw, _groupingType)
                                    ? "Successfully exported"
                                    : "Export didn't complete successfully");

                bw.RunWorkerCompleted += (o, e) => Unlock();

                bw.WorkerReportsProgress = true;
                bw.ProgressChanged += (o, e) =>
                                          {
                                              Progress = e.ProgressPercentage;
                                          };

                LockIn();
                Progress = 0;
                bw.RunWorkerAsync();
            }
        }

        public void UseCenteredGrouping()
        {
            _groupingType = GroupingType.Centered;
        }

        public void UseForwardGrouping()
        {
            _groupingType = GroupingType.Forward;
        }

        public void Closing(CancelEventArgs eventArgs)
        {
            if (_lockedIn)
                eventArgs.Cancel = true;
        }

        private void LockIn()
        {
            LockedIn = true;
            ApplicationCursor = Cursors.Wait;
        }

        private void Unlock()
        {
            LockedIn = false;
            ApplicationCursor = Cursors.Arrow;
        }

        private void LoadCSV(string filename)
        {
            if (!filename.EndsWith(".csv"))
                throw new ArgumentException("Can't tell if this is a csv (filetype not .csv)");

            if (!File.Exists(filename))
                throw new ArgumentException("File wasn't found");

            Series = new List<Series>();
            _timestamps = new List<DateTime>();

            using (var reader = new StreamReader(filename))
            {
                var linesRead = 0;
                var csvHeaderLine = reader.ReadLine();
                linesRead++;
                if (csvHeaderLine == null)
                    throw new FileFormatException("Couldn't read the header line!");

                var headers = csvHeaderLine.Split(',');

                if (headers.Distinct().Count() < headers.Count())
                    throw new FileFormatException("There are duplicate headers!");

                var dateTimeComponents = new List<DateTimeComponent>();

                foreach (var header in headers)
                {
                    DateTimeComponentType dateTimeComponent;
                    var cleansedHeader = header.Replace(" ", "").Replace("-", "").Replace("/", "").Replace(":", "");
                    var isDateTimeComponent = Enum.TryParse(cleansedHeader, out dateTimeComponent);
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
                        Series.Add(new Series(header)
                        {
                            ColumnIndex = Array.IndexOf(headers, header)
                        });
                    }
                }

                string lineRead;
                while ((lineRead = reader.ReadLine()) != null)
                {
                    linesRead++;
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
                            switch (dateTimeComponent.Type)
                            {
                                case DateTimeComponentType.DD:
                                    dateTime = new DateTime(dateTime.Year, dateTime.Month, int.Parse(cleansedLineComponenets[dateTimeComponent.Index]), dateTime.Hour, dateTime.Minute, 0);
                                    hasDay = true;
                                    break;
                                case DateTimeComponentType.DDMM:
                                    dateTime = new DateTime(dateTime.Year, int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 2)), dateTime.Hour, dateTime.Minute, 0);
                                    hasDay = true;
                                    hasMonth = true;
                                    break;
                                case DateTimeComponentType.DDMMYYYY:
                                    dateTime = new DateTime(int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(4)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(2, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 2)), dateTime.Hour, dateTime.Minute, 0);
                                    hasDay = true;
                                    hasMonth = true;
                                    hasYear = true;
                                    break;
                                case DateTimeComponentType.DDMMYYYYhhmm:
                                    dateTime = new DateTime(int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(4, 4)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(2, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(8, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(10, 2)), 0);
                                    hasDay = true;
                                    hasMonth = true;
                                    hasYear = true;
                                    hasHour = true;
                                    hasMinute = true;
                                    break;
                                case DateTimeComponentType.MM:
                                    dateTime = new DateTime(dateTime.Year, int.Parse(cleansedLineComponenets[dateTimeComponent.Index]), dateTime.Day, dateTime.Hour, dateTime.Minute, 0);
                                    hasMonth = true;
                                    break;
                                case DateTimeComponentType.MMDD:
                                    dateTime = new DateTime(dateTime.Year, int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(2)), dateTime.Hour, dateTime.Minute, 0);
                                    hasMonth = true;
                                    hasDay = true;
                                    break;
                                case DateTimeComponentType.MMDDYYYY:
                                    dateTime = new DateTime(int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(4)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(2, 2)), dateTime.Hour, dateTime.Minute, 0);
                                    hasMonth = true;
                                    hasDay = true;
                                    hasYear = true;
                                    break;
                                case DateTimeComponentType.MMDDYYYYhhmm:
                                    dateTime = new DateTime(int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(4, 4)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(2, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(8, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(10, 2)), 0);
                                    hasMonth = true;
                                    hasDay = true;
                                    hasYear = true;
                                    hasHour = true;
                                    hasMinute = true;
                                    break;
                                case DateTimeComponentType.YYYY:
                                    dateTime = new DateTime(int.Parse(cleansedLineComponenets[dateTimeComponent.Index]), dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0);
                                    hasYear = true;
                                    break;
                                case DateTimeComponentType.YYYYDDMM:
                                    dateTime = new DateTime(int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 4)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(6)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(4, 2)), dateTime.Hour, dateTime.Minute, 0);
                                    hasYear = true;
                                    hasDay = true;
                                    hasMonth = true;
                                    break;
                                case DateTimeComponentType.YYYYDDMMhh:
                                    dateTime = new DateTime(int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 4)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(6, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(4, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(8)), dateTime.Minute, 0);
                                    hasYear = true;
                                    hasDay = true;
                                    hasMonth = true;
                                    hasHour = true;
                                    break;
                                case DateTimeComponentType.YYYYDDMMhhmm:
                                    dateTime = new DateTime(int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 4)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(6, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(4, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(8, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(10)), 0);
                                    hasYear = true;
                                    hasDay = true;
                                    hasMonth = true;
                                    hasHour = true;
                                    hasMinute = true;
                                    break;
                                case DateTimeComponentType.YYYYMM:
                                    dateTime = new DateTime(int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 4)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(4)), dateTime.Day, dateTime.Hour, dateTime.Minute, 0);
                                    hasYear = true;
                                    hasMonth = true;
                                    break;
                                case DateTimeComponentType.YYYYMMDD:
                                    dateTime = new DateTime(int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 4)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(4, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(6)), dateTime.Hour, dateTime.Minute, 0);
                                    hasYear = true;
                                    hasMonth = true;
                                    hasDay = true;
                                    break;
                                case DateTimeComponentType.YYYYMMDDhh:
                                    dateTime = new DateTime(int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 4)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(4, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(6, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(8)), dateTime.Minute, 0);
                                    hasYear = true;
                                    hasMonth = true;
                                    hasDay = true;
                                    hasHour = true;
                                    break;
                                case DateTimeComponentType.YYYYMMDDhhmm:
                                    dateTime = new DateTime(int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 4)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(4, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(6, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(8, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(10)), 0);
                                    hasYear = true;
                                    hasMonth = true;
                                    hasDay = true;
                                    hasHour = true;
                                    hasMinute = true;
                                    break;
                                case DateTimeComponentType.hh:
                                    dateTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, int.Parse(cleansedLineComponenets[dateTimeComponent.Index]), dateTime.Minute, 0);
                                    hasHour = true;
                                    break;
                                case DateTimeComponentType.hhmm:
                                    dateTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(0, 2)), int.Parse(cleansedLineComponenets[dateTimeComponent.Index].Substring(2)), 0);
                                    hasHour = true;
                                    hasMinute = true;
                                    break;
                                case DateTimeComponentType.mm:
                                    dateTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, int.Parse(cleansedLineComponenets[dateTimeComponent.Index]), 0);
                                    hasMinute = true;
                                    break;
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

                    _timestamps.Add(dateTime);

                    foreach (var series in Series)
                    {
                        if (series.ColumnIndex > lineComponents.Length)
                            throw new FileFormatException("There aren't enough values for all the sensors");

                        float value;
                        if (float.TryParse(lineComponents[series.ColumnIndex], out value))
                            series.Values[dateTime] = value;
                    }
                }
            }
            Series = new List<Series>(Series);
        }
    }

    public class DateTimeComponent
    {
        public int Index;
        public DateTimeComponentType Type;
    }
}
