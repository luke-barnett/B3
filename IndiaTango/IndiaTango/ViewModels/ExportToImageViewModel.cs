﻿using System;
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

        public string Filename { get { return _filename; } set { _filename = value; NotifyOfPropertyChange(() => Filename); } }

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
                return;

            Common.RenderChartToImage(Chart, SelectedSensors, _width, _height, _renderAllPoints, Filename);
            TryClose();
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
