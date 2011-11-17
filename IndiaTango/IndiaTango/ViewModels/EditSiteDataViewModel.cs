using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Caliburn.Micro;
using IndiaTango.Models;
using Cursors = System.Windows.Input.Cursors;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using Image = System.Windows.Controls.Image;
using Orientation = System.Windows.Controls.Orientation;

namespace IndiaTango.ViewModels
{
    public class EditSiteDataViewModel : BaseViewModel
    {
        public EditSiteDataViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;

            AllContacts = Contact.ImportAll();
        }

        #region Private Parameters

        private readonly IWindowManager _windowManager;
        private readonly SimpleContainer _container;
        private Dataset _dataSet;
        private bool _siteControlsEnabled;
        private ObservableCollection<Contact> _allContacts;
        private bool _hasSelectedPrimaryContact;
        private bool _hasSelectedSecondaryContact;
        private bool _hasSelectedUniversityContact;
        private Visibility _doneCancelVisible;
        private int _selectedImage;

        #region Site Details

        private string _siteName;
        private string _owner;
        private Contact _primaryContact;
        private Contact _secondaryContact;
        private Contact _universityContact;
        private string _latitude;
        private string _longitude;
        private List<NamedBitmap> _siteImages;

        #endregion

        #endregion

        #region Public Parameters

        #endregion

        #region Private Properties

        private List<NamedBitmap> SiteImages
        {
            get { return _siteImages; }
            set
            {
                _siteImages = value;
                NotifyOfPropertyChange(() => SiteImages);
                NotifyOfPropertyChange(() => StackPanelSiteImages);
            }
        }

        #endregion

        #region Public Properties

        public Dataset DataSet
        {
            get { return _dataSet; }
            set
            {
                _dataSet = value;

                if (DataSet.Site != null)
                {
                    SiteName = DataSet.Site.Name;
                    Owner = DataSet.Site.Owner;
                    PrimaryContact = DataSet.Site.PrimaryContact;
                    SecondaryContact = DataSet.Site.SecondaryContact;
                    UniversityContact = DataSet.Site.UniversityContact;
                    if (DataSet.Site.GpsLocation != null)
                    {
                        Latitude = DataSet.Site.GpsLocation.DecimalDegreesLatitude.ToString();
                        Longitude = DataSet.Site.GpsLocation.DecimalDegreesLongitude.ToString();
                    }
                    else
                    {
                        Latitude = "0";
                        Longitude = "0";
                    }

                    SiteImages = DataSet.Site.Images ?? new List<NamedBitmap>();
                }
                else
                {
                    SiteName = "";
                    Owner = "";
                    Latitude = "0";
                    Longitude = "0";
                    PrimaryContact = null;
                    SecondaryContact = null;
                    UniversityContact = null;
                    SiteImages = new List<NamedBitmap>();
                }
            }
        }

        public string Title
        {
            get { return string.Format("Site: {0}", SiteName); }
        }

        public bool SiteControlsEnabled
        {
            get { return _siteControlsEnabled; }
            set
            {
                _siteControlsEnabled = value;
                NotifyOfPropertyChange(() => SiteControlsEnabled);
            }
        }

        public ObservableCollection<Contact> AllContacts
        {
            get { return _allContacts; }
            set { _allContacts = value; NotifyOfPropertyChange(() => AllContacts); }
        }

        public bool HasSelectedPrimaryContact
        {
            get { return _hasSelectedPrimaryContact; }
            set
            {
                _hasSelectedPrimaryContact = value;
                NotifyOfPropertyChange(() => HasSelectedPrimaryContact);
            }
        }

        public bool HasSelectedSecondaryContact
        {
            get { return _hasSelectedSecondaryContact; }
            set
            {
                _hasSelectedSecondaryContact = value;
                NotifyOfPropertyChange(() => HasSelectedSecondaryContact);
            }
        }

        public bool HasSelectedUniversityContact
        {
            get { return _hasSelectedUniversityContact; }
            set
            {
                _hasSelectedUniversityContact = value;
                NotifyOfPropertyChange(() => HasSelectedUniversityContact);
            }
        }

        public Visibility DoneCancelVisible
        {
            get { return _doneCancelVisible; }
            set
            {
                _doneCancelVisible = value;
                NotifyOfPropertyChange(() => DoneCancelVisible);
                NotifyOfPropertyChange(() => SiteListEnabled);
                NotifyOfPropertyChange(() => CanDragDropImages);
            }
        }

        public bool EditDeleteEnabled
        {
            get { return true; }
        }

        public Visibility EditDeleteVisible
        {
            get { return Visibility.Visible; }
        }

        public bool SiteListEnabled
        {
            get { return DoneCancelVisible != Visibility.Visible; }
        }

        public bool CanDragDropImages
        {
            get { return DoneCancelVisible == Visibility.Visible; }
        }

        public int SelectedImage
        {
            get { return _selectedImage; }
            set { _selectedImage = value; NotifyOfPropertyChange(() => SelectedImage); }
        }

        #region Site Details

        public string SiteName
        {
            get { return _siteName; }
            set
            {
                _siteName = value;
                NotifyOfPropertyChange(() => SiteName);
            }
        }

        public string Owner
        {
            get { return _owner; }
            set
            {
                _owner = value;
                NotifyOfPropertyChange(() => Owner);
            }
        }

        public Contact PrimaryContact
        {
            get { return _primaryContact; }
            set
            {
                _primaryContact = value;
                NotifyOfPropertyChange(() => PrimaryContact);
            }
        }

        public Contact SecondaryContact
        {
            get { return _secondaryContact; }
            set
            {
                _secondaryContact = value;
                NotifyOfPropertyChange(() => SecondaryContact);
            }
        }

        public Contact UniversityContact
        {
            get { return _universityContact; }
            set
            {
                _universityContact = value;
                NotifyOfPropertyChange(() => UniversityContact);
            }
        }

        public string Latitude
        {
            get { return _latitude; }
            set
            {
                _latitude = value;
                NotifyOfPropertyChange(() => Latitude);
            }
        }

        public string Longitude
        {
            get { return _longitude; }
            set
            {
                _longitude = value;
                NotifyOfPropertyChange(() => Longitude);
            }
        }

        public List<StackPanel> StackPanelSiteImages
        {
            get
            {
                var list = new List<StackPanel>();

                if (_siteImages != null)
                {
                    list.AddRange(_siteImages.Select(MakeImageItem));
                }

                return list;
            }
        }

        #endregion

        #endregion

        #region Private Methods

        private static StackPanel MakeImageItem(NamedBitmap bitmap)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            var image = new Image { Source = Common.BitmapToImageSource(bitmap.Bitmap), Height = 50, Width = 50 };
            image.MouseLeftButtonUp += delegate
            {
                var path = Path.Combine(Common.TempDataPath, bitmap.Name);
                if (!File.Exists(path))
                    bitmap.Bitmap.Save(path);
                System.Diagnostics.Process.Start(path);
            };
            image.Cursor = Cursors.Hand;
            image.Margin = new Thickness(3);
            var text = new TextBlock
                           {
                               Text = bitmap.Name,
                               Margin = new Thickness(5, 0, 0, 0),
                               VerticalAlignment = VerticalAlignment.Center
                           };
            panel.Children.Add(image);
            panel.Children.Add(text);

            return panel;
        }

        private void InsertImagesForSite(IEnumerable<string> fileNames)
        {
            if (_siteImages == null)
                _siteImages = new List<NamedBitmap>();

            foreach (var img in from fileName in fileNames where File.Exists(fileName) select new NamedBitmap(new Bitmap(fileName), Path.GetFileName(fileName)))
            {
                _siteImages.Add(img);
            }

            NotifyOfPropertyChange(() => SiteImages);
        }

        #endregion

        #region Public Methods

        #endregion

        #region Event Handlers

        #region Primary Contact

        public void BtnNewPrimary()
        {

        }

        public void BtnEditPrimary()
        {

        }

        public void BtnDelPrimary()
        {

        }

        #endregion

        #region Secondary Contact

        public void BtnNewSecondary()
        {

        }

        public void BtnEditSecondary()
        {

        }

        public void BtnDelSecondary()
        {

        }

        #endregion

        #region University Contact

        public void BtnNewUni()
        {

        }

        public void BtnEditUni()
        {

        }

        public void BtnDelUni()
        {

        }

        #endregion

        #region Images

        public void BtnNewImage()
        {
            var dlg = new OpenFileDialog
                          {
                              Filter =
                                  @"Image Files|*.jpeg;*.jpg;*.png;*.gif;*.bmp|JPEG Files (*.jpeg)|*.jpeg|JPG Files (*.jpg)|*.jpg|PNG Files (*.png)|*.png|GIF Files (*.gif)|*.gif|Bitmap Files|*.bmp",
                              Title = @"Select images to add",
                              Multiselect = true
                          };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                InsertImagesForSite(dlg.FileNames);
            }
        }

        public void BtnDeleteImage()
        {
            if (SelectedImage == -1 || !Common.ShowMessageBox("Confirm Delete", "Are you sure you wish to delete this image?", true, false))
                return;

            _siteImages.RemoveAt(SelectedImage);
            NotifyOfPropertyChange(() => SiteImages);
        }

        #region Image Dragging

        public void StartImageDrag(DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        public void DoImageDrag(DragEventArgs e)
        {
            var data = (string[])e.Data.GetData(DataFormats.FileDrop);

            var acceptedExtensions = new List<string> { ".jpeg", ".jpg", ".png", ".gif", ".bmp" };

            InsertImagesForSite((from file in data let extension = Path.GetExtension(file) where extension != null && acceptedExtensions.Contains(extension.ToLower()) select file).ToArray());
        }

        #endregion

        #endregion

        public void BtnSiteDone()
        {
            
        }

        public void BtnSiteCancel()
        {
            
        }

        public void BtnSiteEdit()
        {
            
        }

        public void BtnSiteDelete()
        {
            
        }


        #endregion
    }
}
