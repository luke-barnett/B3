using System.Diagnostics;
using System.Windows.Input;
using IndiaTango.Models;
using Screen = Caliburn.Micro.Screen;

namespace IndiaTango.ViewModels
{
    public class BaseViewModel : Screen
    {
        public string Version { get { return Common.Version; } }
        public string Creators { get { return Common.Creators; } }
        public string Icon { get { return Common.Icon; } }
        public string ApplicationTitle { get { return Common.ApplicationTitle; } }
		public string ApplicationTagLine { get { return Common.TagLine; } }

        private Cursor _cursor = Cursors.Arrow;
        public Cursor ApplicationCursor { get { return _cursor; } set { _cursor = value; NotifyOfPropertyChange(() => ApplicationCursor); } }
    }
}
