
using System.Reflection;
using System.Windows;

namespace IndiaTango.ViewModels
{
    public class MainViewModel
    {

        public string Title { get { return "INDIA TANGO"; } }
        public string Version { get { return string.Format("[alpha version {0}]", Assembly.GetExecutingAssembly().GetName().Version.ToString()); }}
        public string Creators { get { return "Developed by:\r\nSteven McTanish\r\nLuke Barnett\r\nMichael Baumberger\r\nKerry Arts"; } }

        

        public void BtnLoad()
        {
            
        }
    }
}
