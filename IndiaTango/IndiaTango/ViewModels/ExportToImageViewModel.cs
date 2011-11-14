using System;
using System.Diagnostics;
using System.Windows.Forms;
using IndiaTango.Models;
using Visiblox.Charts;

namespace IndiaTango.ViewModels
{
    public class ExportToImageViewModel : BaseViewModel
    {
        private string _filename;
        private int _width = 1600;
        private int _height = 1200;
        private bool _renderAllPoints = true;

        public Chart Chart { get; set; }
        public GraphableSensor[] SelectedSensors { get; set; }

        public string Filename { get { return string.IsNullOrWhiteSpace(_filename) ? "No filename selected" : _filename; } set { _filename = value; NotifyOfPropertyChange(() => Filename); } }

        public string WindowTitle { get { return "Export to Image"; } }

        public string WidthTextBox
        {
            get { return _width.ToString(); }
            set
            {
                try
                {
                    _width = int.Parse(value);
                }
                catch
                {

                }
                _width = Math.Abs(_width);
                NotifyOfPropertyChange(() => WidthTextBox);
            }
        }

        public string HeightTextBox
        {
            get { return _height.ToString(); }
            set
            {
                try
                {
                    _height = int.Parse(value);
                }
                catch
                {

                }
                _height = Math.Abs(_height);
                NotifyOfPropertyChange(() => HeightTextBox);
            }
        }

        public bool RenderAllPoints { get { return _renderAllPoints; } set { _renderAllPoints = value; NotifyOfPropertyChange(() => RenderAllPoints); } }

        public void SaveImage()
        {
            if (string.IsNullOrWhiteSpace(_filename))
            {
                Common.ShowMessageBox("Filename is not set", "Please set a filename for the image and try again", false,
                                      false);
                return;
            }

            try
            {
                Common.RenderChartToImage(Chart, SelectedSensors, _width, _height, _renderAllPoints, Filename);
                TryClose();
            }
            catch(Exception e)
            {
                Common.ShowMessageBoxWithException("Exporting Image Failed",
                                                   "Sorry something went wrong when we where exporting to an image",
                                                   false, true, e);
                EventLogger.LogError(null, "Image Exporter", e.Message);
            }
        }

        public void ShowFileDialog()
        {
            Debug.WriteLine("Showing File Dialog");
            var fileDialog = new SaveFileDialog
            {
                AddExtension = true,
                Filter = @"Images|*.png"
            };

            var result = fileDialog.ShowDialog();

            if (result == DialogResult.OK)
                Filename = fileDialog.FileName;
        }

        public void DoRenderAllPoints()
        {
            RenderAllPoints = true;
        }

        public void DontRenderAllPoints()
        {
            RenderAllPoints = false;
        }
    }
}
