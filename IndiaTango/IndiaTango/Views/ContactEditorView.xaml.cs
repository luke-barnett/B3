using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using IndiaTango.Models;

namespace IndiaTango.Views
{
    /// <summary>
    /// Interaction logic for ContactEditorView.xaml
    /// </summary>
    public partial class ContactEditorView : Window
    {
        public ContactEditorView()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtTitle.Focus();
            Keyboard.Focus(txtTitle);
        }
    }
}
