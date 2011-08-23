using System.Windows.Forms;
using Caliburn.Micro;

namespace IndiaTango.ViewModels
{
    class ContactEditorViewModel : BaseViewModel
    {
        private readonly IWindowManager _windowManager = null;
        private readonly SimpleContainer _container = null;

        public ContactEditorViewModel(IWindowManager manager, SimpleContainer container)
        {
            _windowManager = manager;
            _container = container;
        }

        public string Title
        {
            get { return "Choose Contact"; }
        }

        public void btnCancel()
        {
            this.TryClose();
        }

        public void btnChoose()
        {
            MessageBox.Show("This will eventually choose this contact.");
        }
    }
}
