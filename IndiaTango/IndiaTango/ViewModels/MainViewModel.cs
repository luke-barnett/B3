using System.Windows;

namespace IndiaTango.ViewModels
{
    public class MainViewModel : BaseViewModel
    {

        public string Title { get { return ApplicationTitle; } }

        public void BtnNew()
        {
            
        }
        
        public void BtnLoad()
        {
            MessageBox.Show("Sorry, not yet implemented");
        }
    }
}
