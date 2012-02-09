using System.Windows.Input;
using IndiaTango.Models;
using Screen = Caliburn.Micro.Screen;

namespace IndiaTango.ViewModels
{
    public class BaseViewModel : Screen
    {
        /// <summary>
        /// The current version of the program
        /// </summary>
        public string Version { get { return Common.Version; } }
        /// <summary>
        /// The creators of the program
        /// </summary>
        public string Creators { get { return Common.Creators; } }
        /// <summary>
        /// The application icon
        /// </summary>
        public string Icon { get { return Common.Icon; } }
        /// <summary>
        /// The application title
        /// </summary>
        public string ApplicationTitle { get { return Common.ApplicationTitle; } }
        /// <summary>
        /// The application tag line
        /// </summary>
		public string ApplicationTagLine { get { return Common.TagLine; } }

        
        private Cursor _cursor = Cursors.Arrow;
        /// <summary>
        /// The cursor to use
        /// </summary>
        public Cursor ApplicationCursor { get { return _cursor; } set { _cursor = value; NotifyOfPropertyChange(() => ApplicationCursor); } }
    }
}
