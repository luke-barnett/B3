using System;
using System.Windows;
using IndiaTango.Models;

namespace IndiaTango.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
        }

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			Common.SetFancyBackground(this,grdMain, true, true);
		}
    }
}
