using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {
        public new string Version
        {
            get { return string.Format("Version: {0}", Common.Version); }
        }

        public string Description
        {
            get { return "B3 is QA/QC Software developed at the University of Waikato..."; }
        }

        public string Developers
        {
            get { return "Luke Barnett, Chris McBride, Steven McTanish, Kerry Arts, Michael Baumberger"; }
        }

        public string Title
        {
            get { return "About B3"; }
        }
    }
}
