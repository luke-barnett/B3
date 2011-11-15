using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using IndiaTango.ViewModels;
using Visiblox.Charts;
using Visiblox.Charts.Primitives;
using WindowsFormsAero.Dwm;
using WindowsFormsAero.TaskDialog;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Cursors = System.Windows.Input.Cursors;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Path = System.IO.Path;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using Size = System.Windows.Size;

namespace IndiaTango.Models
{
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
        private static List<string> _changeReasons;

        public static string ChangeReasonsPath { get { return Path.Combine(AppDataPath, "ChangeReasons.txt"); } }
        public static string Icon { get { return "/B3;component/Images/icon.ico"; } }
        public static string TestDataPath { get { return "../../Test Data/"; } }
        public static string AppDataPath
        {
            get
            {
                var path = Path.Combine(new[]{
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "IndiaTango"
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

        public static bool CanUseGlass = WindowsFormsAero.OsSupport.IsVistaOrBetter &&
                                         WindowsFormsAero.OsSupport.IsCompositionEnabled;

        public static string AddIcon { get { return "/B3;component/Images/plus.png"; } }
        public static string EditIcon { get { return "/B3;component/Images/pencil.png"; } }
        public static string DeleteIcon { get { return "/B3;component/Images/cross-script.png"; } }

        public static void SetFancyBackground(Window window, Grid mainGrid, bool useGlass, bool useGradient)
        {
            //Set the glass
            if (useGlass && CanUseGlass)
            {
                //Get handle
                IntPtr hwnd = new WindowInteropHelper(window).Handle;

                //Set backgroung to black and transparent (At the same time!!!)
                window.Background = Brushes.Transparent;
                HwndSource source = HwndSource.FromHwnd(hwnd);
                if (source != null && source.CompositionTarget != null) source.CompositionTarget.BackgroundColor = Color.FromArgb(0, 0, 0, 0);

                //Set glass
                DwmManager.EnableBlurBehind(hwnd);
            }

            //Create rectangle and set its position and span
            var rectangle = new Rectangle();
            Grid.SetColumn(rectangle, 0);
            Grid.SetRow(rectangle, 0);
            Grid.SetColumnSpan(rectangle, mainGrid.ColumnDefinitions.Count > 0 ? mainGrid.ColumnDefinitions.Count : 1);
            Grid.SetRowSpan(rectangle, mainGrid.RowDefinitions.Count > 0 ? mainGrid.RowDefinitions.Count : 1);

            if (useGradient)
            {
                //Create the gradient brush
                var gradients = new GradientStopCollection();
                if (useGlass && CanUseGlass)
                {
                    gradients.Add(new GradientStop(Colors.White, 0));
                    gradients.Add(new GradientStop(Color.FromArgb(150, 255, 255, 255), 1));
                }
                else
                {
                    gradients.Add(new GradientStop(Colors.White, 0));
                    gradients.Add(new GradientStop(Colors.White, 0.5));
                    gradients.Add(new GradientStop(Color.FromRgb(220, 220, 220), 1));
                }

                rectangle.Fill = new LinearGradientBrush(gradients, new Point(1, 0), new Point(1, 1));
            }
            else
            {
                rectangle.Fill = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));
            }

            //Insert the rectangle
            mainGrid.Children.Insert(0, rectangle);
        }

        public static bool ShowMessageBox(string title, string text, bool showCancel, bool isError)
        {
            if (CanUseGlass && !Debugger.IsAttached)
            {
                var dialog = new TaskDialog(title, title, text,
                                                   (showCancel ? (TaskDialogButton.OK | TaskDialogButton.Cancel) : TaskDialogButton.OK));
                try
                {
                    dialog.CustomIcon = isError ? Properties.Resources.error_32 : Properties.Resources.info_32;
                    var result = dialog.Show();
                    return result.CommonButton == Result.OK;
                }
                catch (Exception e)
                {
                    System.Windows.Forms.MessageBox.Show(e.Message);
                }
                return false;
            }
            else
            {
                return System.Windows.Forms.MessageBox.Show(text, title,
                    showCancel ? MessageBoxButtons.OKCancel : MessageBoxButtons.OK, isError ? MessageBoxIcon.Error : MessageBoxIcon.Information) == DialogResult.OK;
            }
        }

        public static bool ShowMessageBoxWithExpansion(string title, string text, bool showCancel, bool isError, string expansion)
        {
            if (CanUseGlass && !Debugger.IsAttached)
            {
                var dialog = new TaskDialog(title, title, text,
                                                   (showCancel ? (TaskDialogButton.OK | TaskDialogButton.Cancel) : TaskDialogButton.OK));
                try
                {
                    dialog.CustomIcon = isError ? Properties.Resources.error_32 : Properties.Resources.info_32;

                    dialog.ShowExpandedInfoInFooter = true;
                    dialog.ExpandedInformation = expansion;

                    var result = dialog.Show();
                    return result.CommonButton == Result.OK;
                }
                catch (Exception e)
                {
                    System.Windows.Forms.MessageBox.Show(e.Message);
                }

                return false;
            }
            else
            {
                return ShowMessageBox(title, text + "\n\n" + expansion, showCancel, isError);
            }
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
            if (CanUseGlass && !Debugger.IsAttached)
            {
                var dialog = new TaskDialog(title, title, message,
                                                   TaskDialogButton.Yes | TaskDialogButton.No,
                                                   TaskDialogIcon.Warning);
                var result = dialog.Show();
                return result.CommonButton == Result.Yes;
            }
            else
            {
                return System.Windows.Forms.MessageBox.Show(message, title, MessageBoxButtons.YesNo,
                                            MessageBoxIcon.Warning) == DialogResult.Yes;
            }
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

        public static void RequestReason(Sensor sensor, SimpleContainer container, IWindowManager windowManager, string taskPerformed)
        {
            RequestReason(new List<Sensor> { sensor }, container, windowManager, taskPerformed);
        }

        public static void RequestReason(List<Sensor> sensors, SimpleContainer container, IWindowManager windowManager, string taskPerformed)
        {
            if (sensors.Count > 0)
            {
                var specify = (SpecifyValueViewModel)container.GetInstance(typeof(SpecifyValueViewModel), "SpecifyValueViewModel");
                specify.Title = "Log Reason";
                specify.Message = "Please specify a reason for this change:";
                specify.ShowComboBox = true;
                specify.ComboBoxItems = GetChangeReasons();
                specify.Deactivated += (o, e) =>
                {
                    foreach (Sensor sensor in sensors)
                    {
                        // Specify reason
                        sensor.CurrentState.Reason = specify.Text;

                        // Log this change to the file!
                        sensor.CurrentState.LogChange(sensor.Name, taskPerformed);
                    }

                    //Add the change to the list
                    AddChangeReason(specify.Text);
                };

                windowManager.ShowDialog(specify);
            }
        }

        public static List<string> GenerateSamplingCaps()
        {
            var samplingCaps = new List<string> { "1000", "5000", "10000", "15000", "20000", "30000", "40000", "All" };

            return samplingCaps;
        }

        public static List<string> GetChangeReasons()
        {
            if (_changeReasons == null)
            {
                _changeReasons = new List<string>();

                if (File.Exists(ChangeReasonsPath))
                {
                    using (var reader = new StreamReader(ChangeReasonsPath))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                            _changeReasons.Add(line);
                    }
                }
            }

            return _changeReasons;
        }

        public static void AddChangeReason(string reason)
        {
            if (String.IsNullOrWhiteSpace(reason) || _changeReasons.Contains(reason))
                return;

            _changeReasons.Add(reason);
            _changeReasons.Sort();

            using (var writer = new StreamWriter(ChangeReasonsPath))
            {
                foreach (string changeReason in _changeReasons)
                    writer.WriteLine(changeReason);
            }

            Debug.WriteLine("Added change reason: '" + reason + "'");
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

            if (string.IsNullOrWhiteSpace(sessionToSave.SaveLocation))
            {
                var saveFileDialog = new SaveFileDialog { Filter = "Site Files|*.b3" };
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    sessionToSave.SaveLocation = saveFileDialog.FileName;
            }

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
    }
}
