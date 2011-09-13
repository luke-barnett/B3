using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Caliburn.Micro;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    class ExportViewModel : BaseViewModel
    {
        private IWindowManager _windowManager;
        private SimpleContainer _container;
        private Dataset _dataset;
        private bool _includeEmptyLines = true;
        private bool _includeMetaData = false;
        private bool _includeChangeLog = false;
        private DateColumnFormat _dateColumnFormat;
        private ExportedPoints _exportedPoints;

        public ExportViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;

            _dateColumnFormat = DateColumnFormat.TwoDateColumn;
            _exportedPoints = ExportedPoints.AllPoints;
        }

        #region View Properties
        public string Title
        {
            get { return "Export Options"; }
        }

        public List<DateColumnFormat> DateColumnFormatOptions
        {
            get { return new List<DateColumnFormat>(new[] { DateColumnFormat.TwoDateColumn, DateColumnFormat.SplitDateColumn }); }
        }

        public List<ExportedPoints> ExportedPointsOptions
        {
            get
            {
                return new List<ExportedPoints>(new[] { ExportedPoints.AllPoints,ExportedPoints.HourlyPoints,
                    ExportedPoints.DailyPoints,ExportedPoints.WeeklyPoints }); 
            }
        }

        public DateColumnFormat DateColumnFormat
        {
            get { return _dateColumnFormat; }
            set { _dateColumnFormat = value; NotifyOfPropertyChange(() => DateColumnFormat); Debug.WriteLine("column format changed: " + value.Name); }
        }

        public ExportedPoints ExportedPoints
        {
            get { return _exportedPoints; }
            set { _exportedPoints = value; NotifyOfPropertyChange(() => ExportedPoints); Debug.WriteLine("export format changed: " + value.Name); }
        }

        public bool IncludeEmptyLines
        {
            get { return _includeEmptyLines; }
            set { _includeEmptyLines = value; NotifyOfPropertyChange(() => IncludeEmptyLines); }
        }

        public bool IncludeMetaData
        {
            get { return _includeMetaData; }
            set { _includeMetaData = value; NotifyOfPropertyChange(() => IncludeMetaData);}
        }

        public bool IncludeChangeLog
        {
            get { return _includeChangeLog; }
            set { _includeChangeLog = value; NotifyOfPropertyChange(() => IncludeChangeLog);}
        }

        public Dataset Dataset
        {
            get { return _dataset; }
            set { _dataset = value; }
        }


        #endregion

        #region Event Handlers
        public void btnExport()
        {
            var dialog = new SaveFileDialog();
            dialog.Filter = ExportFormat.CSV.FilterText;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                DatasetExporter exporter = new DatasetExporter(Dataset);
                exporter.Export(dialog.FileName, ExportFormat.CSV, IncludeEmptyLines, IncludeMetaData, IncludeChangeLog, ExportedPoints, DateColumnFormat,1);
                this.TryClose();
            }
        }

        public void btnCancel()
        {
            this.TryClose();
        }
    
        #endregion
    }

    
}
