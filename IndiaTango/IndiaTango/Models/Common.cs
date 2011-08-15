using System.Reflection;

namespace IndiaTango.Models
{
    public static class Common
    {
        public static string ApplicationTitle { get { return "INDIA TANGO"; } }
        public static string Version { get { return string.Format("[alpha version {0}]", Assembly.GetExecutingAssembly().GetName().Version.ToString()); } }
        public static string Creators { get { return "Developed by:\r\nSteven McTanish\r\nLuke Barnett\r\nMichael Baumberger\r\nKerry Arts"; } }

        public static string Icon { get { return "/IndiaTango;component/Images/icon.ico"; } }
    }
}
