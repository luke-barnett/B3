using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using IndiaTango.ViewModels;
using Visiblox.Charts;
using Visiblox.Charts.Primitives;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Path = System.IO.Path;
using Size = System.Windows.Size;

namespace IndiaTango.Models
{
    /// <summary>
    /// Set of commonly used values and methods
    /// </summary>
    public static class Common
    {
        [DllImport("gdi32")]
        public static extern int DeleteObject(IntPtr hObject);

        public static string TagLine { get { return "QAQC for boys"; } }
        public static string ApplicationTitle { get { return "B3"; } }
        public static string Version { get { return string.Format("alpha version {0}", Assembly.GetExecutingAssembly().GetName().Version); } }
        public static string Creators { get { return "Developed by:\r\nSteven McTainsh\r\nLuke Barnett\r\nMichael Baumberger\r\nKerry Arts"; } }

        public static int MaximumGraphablePoints = 15000;

        public static bool HasInitdTaskDlgs;

        public static string ChangeReasonsPath { get { return Path.Combine(AppDataPath, "ChangeReasons.txt"); } }
        public static string Icon { get { return "/B3;component/Images/icon.ico"; } }
        public static string TestDataPath { get { return "../../Test Data/"; } }
        public static string DatasetSaveLocation
        {
            get
            {
                if (!Directory.Exists(Path.Combine(AppDataPath, "Sites")))
                    Directory.CreateDirectory(Path.Combine(AppDataPath, "Sites"));
                return Path.Combine(AppDataPath, "Sites");
            }
        }

        public static string DatasetExportRootFolder(Dataset dataset)
        {
            var directory = Path.Combine(AppDataPath, "Backups", "Exports", dataset.Site.Name);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            return directory;
        }

        public static string DatasetExportLocation(Dataset dataset)
        {
            var timestamp = DateTime.Now;
            var directory = Path.Combine(AppDataPath, "Backups", "Exports", dataset.Site.Name, timestamp.ToString("yyyy"), timestamp.ToString("MM"), timestamp.ToString("dd"));

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            
            return Path.Combine(directory, timestamp.ToString("HHmmss"));
        }
        public static string AppDataPath
        {
            get
            {
                var path = Path.Combine(new[]{
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "B3"
                });

                if (!Directory.Exists(path)) // Creates directory if it doesn't exist, no need to create beforehand
                    Directory.CreateDirectory(path);

                return path;
            }
        }

        public static string TempDataPath
        {
            get
            {
                string path = Path.Combine(Path.GetTempPath(), "IndiaTango");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                return path;
            }
        }

        public static string AddIcon { get { return "/B3;component/Images/plus.png"; } }
        public static string EditIcon { get { return "/B3;component/Images/pencil.png"; } }
        public static string DeleteIcon { get { return "/B3;component/Images/cross-script.png"; } }

        public static bool ShowMessageBox(string title, string text, bool showCancel, bool isError)
        {
            return System.Windows.Forms.MessageBox.Show(text, title,
                    showCancel ? MessageBoxButtons.OKCancel : MessageBoxButtons.OK, isError ? MessageBoxIcon.Error : MessageBoxIcon.Information) == DialogResult.OK;
        }

        public static bool ShowMessageBoxWithExpansion(string title, string text, bool showCancel, bool isError, string expansion)
        {
            return ShowMessageBox(title, text + "\n\n" + expansion, showCancel, isError);
        }

        public static bool ShowMessageBoxWithException(string title, string text, bool showCancel, bool isError, Exception ex)
        {
            EventLogger.LogError(null, "MessageBoxWithException", "An exception was thrown. " + ex.Message);

            return ShowMessageBoxWithExpansion(title, text, showCancel, isError, ex.Message);
        }

        public static void ShowFeatureNotImplementedMessageBox()
        {
            ShowMessageBox("Feature Not Implemented", "Sorry, this feature has not been created yet.", false, false);
        }

        public static Random Generator = new Random();
        public const string UnknownSite = "Unidentified Site";

        public static bool Confirm(string title, string message)
        {
            return System.Windows.Forms.MessageBox.Show(message, title, MessageBoxButtons.YesNo,
                                            MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        public static void RenderChartToImage(Chart elementToRender, GraphableSensor[] sensors, int width, int height, bool renderFullDataSeries, string filename)
        {
            if (elementToRender == null)
                return;
            Debug.WriteLine("Turning on immediate invalidate");
            //Force immediate invalidation
            InvalidationHandler.ForceImmediateInvalidate = true;

            Debug.WriteLine("Creating new chart");
            var clone = new Chart();

            clone.Width = clone.Height = double.NaN;
            clone.HorizontalAlignment = HorizontalAlignment.Stretch;
            clone.VerticalAlignment = VerticalAlignment.Stretch;
            clone.Margin = new Thickness();

            clone.Title = elementToRender.Title;
            clone.XAxis = new DateTimeAxis { Title = "Date" };
            clone.YAxis = new LinearAxis { Range = (IRange<double>)elementToRender.YAxis.Range, Title = elementToRender.YAxis.Title };

            for (var i = 0; i < elementToRender.Series.Count; i++)
            {
                var series = elementToRender.Series[i];
                var sensor = sensors[i];

                if (sensor.Sensor.Name != series.DataSeries.Title)
                {
                    Debug.WriteLine("Mismatched titles! Oh Dear!");
                    continue;
                }

                var lineSeries = new LineSeries
                                     {
                                         LineStroke = ((LineSeries)series).LineStroke,
                                         DataSeries =
                                             renderFullDataSeries
                                                 ? new DataSeries<DateTime, float>(sensor.Sensor.Name, sensor.DataPoints)
                                                 : series.DataSeries
                                     };
                clone.Series.Add(lineSeries);
            }

            var size = new Size(width, height);

            Debug.WriteLine("Rendering new chart of size {0}", size);
            clone.Measure(size);
            clone.Arrange(new Rect(size));
            clone.UpdateLayout();

            var renderer = new RenderTargetBitmap(width, height, 96d, 96d, PixelFormats.Pbgra32);

            renderer.Render(clone);

            var pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(renderer));

            Debug.WriteLine("Saving to file");
            using (var file = File.Create(filename))
            {
                pngEncoder.Save(file);
            }

            Debug.WriteLine("Turning off immediate invalidate");
            //Reset the invalidation handler
            InvalidationHandler.ForceImmediateInvalidate = false;

            EventLogger.LogInfo(null, "Image Exporter", "Saved graph as image to: " + filename);
        }

        public static ChangeReason RequestReason(SimpleContainer container, IWindowManager windowManager, int defaultReasonNumber)
        {
            var specify = (SpecifyValueViewModel)container.GetInstance(typeof(SpecifyValueViewModel), "SpecifyValueViewModel");
            specify.Title = "Log Reason";
            specify.Message = "Please specify a reason for this change:";
            specify.ShowComboBox = true;
            specify.ComboBoxItems = ChangeReason.ChangeReasons.Where(x => !x.Reason.StartsWith("[Importer]")).Select(x => x.Reason).ToList();
            specify.ShowCancel = true;

            var defaultReason = ChangeReason.ChangeReasons.FirstOrDefault(x => x.ID == defaultReasonNumber);

            if (defaultReason != null)
                specify.ComboBoxSelectedIndex = specify.ComboBoxItems.IndexOf(defaultReason.Reason);

            windowManager.ShowDialog(specify);

            if (specify.WasCanceled)
                return null;

            return ChangeReason.ChangeReasons.FirstOrDefault(x => x.Reason == specify.Text) ?? ChangeReason.AddNewChangeReason(specify.Text);
        }

        public static List<string> GenerateSamplingCaps()
        {
            var samplingCaps = new List<string> { "1000", "5000", "10000", "15000", "20000", "30000", "40000", "All" };

            return samplingCaps;
        }

        public static ImageSource BitmapToImageSource(Bitmap bitmap)
        {
            var hbitmap = bitmap.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(hbitmap, IntPtr.Zero,
                    Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(bitmap.Width, bitmap.Height));
            }
            finally
            {
                DeleteObject(hbitmap);
            }
        }

        public static void SaveSession(BackgroundWorker delegatedBackgroundWorker, Dataset sessionToSave)
        {
            EventLogger.LogInfo(sessionToSave, "Save daemon", "Session save started.");

            if (delegatedBackgroundWorker == null)
                delegatedBackgroundWorker = new BackgroundWorker();

            /*if (string.IsNullOrWhiteSpace(sessionToSave.SaveLocation))
            {
                var saveFileDialog = new SaveFileDialog { Filter = "Site Files|*.b3" };
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    sessionToSave.SaveLocation = saveFileDialog.FileName;
            }*/

            if (!string.IsNullOrWhiteSpace(sessionToSave.SaveLocation))
            {
                delegatedBackgroundWorker.DoWork += (o, e) =>
                                 {
                                     using (var stream = new FileStream(sessionToSave.SaveLocation, FileMode.Create))
                                         new BinaryFormatter().Serialize(stream, sessionToSave);
                                     EventLogger.LogInfo(sessionToSave, "Save [Background Worker]", string.Format("Session save complete. File saved to: {0}", sessionToSave.SaveLocation));
                                 };
            }
            else
                EventLogger.LogInfo(sessionToSave, "Save daemon", "Session save aborted");
            delegatedBackgroundWorker.RunWorkerAsync();
        }

        public static TimeSpan Round(this TimeSpan time, TimeSpan roundingInterval, MidpointRounding roundingType)
        {
            return new TimeSpan(
                Convert.ToInt64(Math.Round(
                    time.Ticks / (decimal)roundingInterval.Ticks,
                    roundingType
                )) * roundingInterval.Ticks
            );
        }

        public static TimeSpan Round(this TimeSpan time, TimeSpan roundingInterval)
        {
            return Round(time, roundingInterval, MidpointRounding.ToEven);
        }

        public static DateTime Round(this DateTime datetime, TimeSpan roundingInterval)
        {
            return new DateTime((datetime - DateTime.MinValue).Round(roundingInterval).Ticks);
        }
    }
}
