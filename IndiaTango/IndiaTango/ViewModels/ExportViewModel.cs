using System;
using System.Collections.Generic;
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

        public string Title
        {
            get { return "Export Options"; }
        }

        public ExportViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
        }

        public Dataset Dataset
        {
            get { return _dataset; }
            set { _dataset = value; }
        }

        public void btnSave()
        {
            var dialog = new SaveFileDialog();
            dialog.Filter = ExportFormat.CSV.FilterText;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                DatasetExporter exporter = new DatasetExporter(Dataset);
                exporter.Export(dialog.FileName, ExportFormat.CSV, true, dialog.FilterIndex == 2);
            }
        }

        public void btnCancel()
        {
            this.TryClose();
        }

    }
}
