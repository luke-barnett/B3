using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    public class BaseViewModel
    {
        public string Version { get { return Common.Version; } }
        public string Creators { get { return Common.Creators; } }
        public string Icon { get { return Common.Icon; } }
        public string ApplicationTitle { get { return Common.ApplicationTitle; } }
    }
}
