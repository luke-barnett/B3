using System.Collections.ObjectModel;
using System.Windows.Forms;
using Caliburn.Micro;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    class ContactEditorViewModel : BaseViewModel
    {
        private readonly IWindowManager _windowManager = null;
        private readonly SimpleContainer _container = null;
    	private Contact _contact;

        public ContactEditorViewModel(IWindowManager manager, SimpleContainer container)
        {
            _windowManager = manager;
            _container = container;
        }

        public string Title
        {
            get { return _contact == null ? "Edit Contact" : "Create New Contact"; }
        }

		public string ContactFirstName { get; set; }

		public string ContactLastName { get; set; }

		public string ContactEmail { get; set; }

		public string ContactPhone { get; set; }

		public string ContactBusiness { get; set; }

		public Contact Contact
		{
			get { return _contact; }
			set
			{
				_contact = value;

				if (_contact != null)
				{
					ContactFirstName = _contact.FirstName;
					ContactLastName = _contact.LastName;
					ContactEmail = _contact.Email;
					ContactPhone = _contact.Phone;
					ContactBusiness = _contact.Business;
				}
				else
				{
					ContactFirstName = "";
					ContactLastName = "";
					ContactEmail = "";
					ContactPhone = "";
					ContactBusiness = "";
				}

				NotifyOfPropertyChange(() => ContactFirstName);
				NotifyOfPropertyChange(() => ContactLastName);
				NotifyOfPropertyChange(() => ContactEmail);
				NotifyOfPropertyChange(() => ContactPhone);
				NotifyOfPropertyChange(() => ContactBusiness);
				NotifyOfPropertyChange(() => CanSave);
			}
		}

    	public bool CanSave
    	{
			get { return _contact != null; }
    	}

        public void btnCancel()
        {
            this.TryClose();
        }

        public void btnSave()
        {
        	Contact.FirstName = ContactFirstName;
        	Contact.LastName = ContactLastName;
        	Contact.Email = ContactEmail;
        	Contact.Business = ContactBusiness;
        	Contact.Phone = ContactPhone;
			this.TryClose();
        }
    }
}
