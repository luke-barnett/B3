using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using Caliburn.Micro;
using IndiaTango.Models;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Cursors = System.Windows.Input.Cursors;
using DataFormats = System.Windows.DataFormats;
using DragEventArgs = System.Windows.DragEventArgs;
using Image = System.Windows.Controls.Image;
using Orientation = System.Windows.Controls.Orientation;
using TextBox = System.Windows.Controls.TextBox;

namespace IndiaTango.ViewModels
{
    class WizardViewModel : BaseViewModel
    {
        #region Private Members
        private readonly SimpleContainer _container;
        private readonly IWindowManager _windowManager;
        private int _currentStep;
        private Dataset _ds;

        private int _currentSensorIndex = 0;
        private Sensor _selectedSensor;
        private bool _errorVisible;

		private bool _siteControlsEnabled;
		private Visibility _createEditDeleteVisible = Visibility.Visible;
		private Visibility _doneCancelVisible = Visibility.Hidden;

        private Contact _primaryContact;
        private Contact _secondaryContact;
        private Contact _universityContact;
        private ObservableCollection<Site> _allSites = new ObservableCollection<Site>();
        private ObservableCollection<Contact> _allContacts = new ObservableCollection<Contact>();
        private List<NamedBitmap> _siteImages = new List<NamedBitmap>();
        private const string StepSeperator = " of ";

        private int _selectedImage = -1;
        #endregion

        public WizardViewModel(SimpleContainer container, IWindowManager windowManager)
        {
            _container = container;
            _windowManager = windowManager;

            _allSites = Site.ImportAll();
            _allContacts = Contact.ImportAll();

            //Hack used to force the damn buttons to update
            DoneCancelVisible = Visibility.Visible;
            DoneCancelVisible = Visibility.Collapsed;
        }

        #region Site View Properties
        public bool EditDeleteEnabled
        {
            get { return SelectedSite != null; }
        }

        public bool SiteListEnabled
        {
            get { return DoneCancelVisible != Visibility.Visible; }
        }

        public bool HasSelectedPrimaryContact
        {
            get { return PrimaryContact != null; }
        }

        public bool HasSelectedSecondaryContact
        {
            get { return SecondaryContact != null; }
        }

        public bool HasSelectedUniContact
        {
            get { return UniversityContact != null; }
        }

        public ObservableCollection<Site> AllSites
        {
            get { return _allSites; }
            set { _allSites = value; NotifyOfPropertyChange(() => AllSites); }
        }

        public ObservableCollection<Contact> AllContacts
        {
            get { return _allContacts; }
            set { _allContacts = value; NotifyOfPropertyChange(() => AllContacts); }
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

        public Visibility CreateEditDeleteVisible
        {
            get { return _createEditDeleteVisible; }
            set
            {

                _createEditDeleteVisible = value;
                NotifyOfPropertyChange(() => CreateEditDeleteVisible);
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

        public int SelectedImage
        {
            get { return _selectedImage; }
            set { _selectedImage = value; NotifyOfPropertyChange(() => SelectedImage); }
        }



        private StackPanel MakeImageItem(NamedBitmap bitmap)
        {
            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;
            Image image = new Image();
            image.Source = Common.BitmapToImageSource(bitmap.Bitmap);
            image.Height = 50;
            image.Width = 50;
            image.MouseLeftButtonUp += delegate
                                           {
                                               string path = Path.Combine(Common.TempDataPath, (string)bitmap.Name);
                                               if (!File.Exists(path))
                                                   bitmap.Bitmap.Save(path);
                                               System.Diagnostics.Process.Start(path);
                                           };
            image.Cursor = Cursors.Hand;
            image.Margin = new Thickness(3);
            TextBlock text = new TextBlock();
            text.Text = (string)bitmap.Name;
            text.Margin = new Thickness(5, 0, 0, 0);
            text.VerticalAlignment = VerticalAlignment.Center;
            panel.Children.Add(image);
            panel.Children.Add(text);

            return panel;
        }
        #endregion

        #region Site Data Properties
        public Site SelectedSite
        {
            get { return _ds.Site; }
            set
            {
                Console.WriteLine("Selected site changed to: " + value);
                _ds.Site = value;
                if (_ds.Site != null)
                {
                    SiteName = _ds.Site.Name; // This is all necessary because we create the Site when we save, not now
                    Owner = _ds.Site.Owner;
                    Latitude = _ds.Site.GpsLocation.DecimalDegreesLatitude.ToString();
                    Longitude = _ds.Site.GpsLocation.DecimalDegreesLongitude.ToString();
                    PrimaryContact = _ds.Site.PrimaryContact;
                    SecondaryContact = _ds.Site.SecondaryContact;
                    UniversityContact = _ds.Site.UniversityContact;

                    if (_ds.Site.Images != null)
                        _siteImages = _ds.Site.Images.ToList();
                    else
                        _siteImages = new List<NamedBitmap>();
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
                    _siteImages = new List<NamedBitmap>();
                }

                NotifyOfPropertyChange(() => SelectedSite);
                NotifyOfPropertyChange(() => SiteName);
                NotifyOfPropertyChange(() => Owner);
                NotifyOfPropertyChange(() => Latitude);
                NotifyOfPropertyChange(() => Longitude);
                NotifyOfPropertyChange(() => PrimaryContact);
                NotifyOfPropertyChange(() => SecondaryContact);
                NotifyOfPropertyChange(() => UniversityContact);
                NotifyOfPropertyChange(() => EditDeleteEnabled);
                NotifyOfPropertyChange(() => SiteImages);
                NotifyOfPropertyChange(() => Title);
            }
        }

        public string SiteName { get; set; }

        public string Owner { get; set; }

        public string Latitude { get; set; }

        public string Longitude { get; set; }

        public Contact PrimaryContact
        {
            get { return _primaryContact; }
            set
            {
                _primaryContact = value;
                NotifyOfPropertyChange(() => PrimaryContact);
                NotifyOfPropertyChange(() => HasSelectedPrimaryContact);
            }
        }

        public Contact SecondaryContact
        {
            get { return _secondaryContact; }
            set
            {
                _secondaryContact = value;
                NotifyOfPropertyChange(() => SecondaryContact);
                NotifyOfPropertyChange(() => HasSelectedSecondaryContact);
            }
        }

        public Contact UniversityContact
        {
            get { return _universityContact; }
            set
            {
                _universityContact = value;
                NotifyOfPropertyChange(() => UniversityContact);
                NotifyOfPropertyChange(() => HasSelectedUniContact);
            }
        }

        public List<StackPanel> SiteImages
        {
            get
            {
                List<StackPanel> list = new List<StackPanel>();

                if (_siteImages != null)
                {
                    foreach (var bitmap in _siteImages)
                        list.Add(MakeImageItem(bitmap));
                }

                return list;
            }
        }

        #endregion

        #region Wizard View Properties
        private const int STEPS_BEFORE_SENSOR_CONFIG = 3;
        private const int STEPS_AFTER_SENSOR_CONFIG = 1;
        private const int SENSOR_CONFIG_STEPS = 2;

        /// <summary>
        /// Gets or sets the zero-based index of the wizard sheet displayed to the user.
        /// </summary>
        public int CurrentStep
        {
            get { return _currentStep; }
            set
            {
                _currentStep = value;
                NotifyOfPropertyChange(() => CurrentStep);
                NotifyOfPropertyChange(() => CanGoBack);
                NotifyOfPropertyChange(() => CanGoForward);
                NotifyOfPropertyChange(() => ThisStep);
            }
        }

        /// <summary>
        /// Gets a string indicating how many steps of the wizard have been completed thus far.
        /// </summary>
        public string ThisStep
        {
            get
            {
                if (CurrentStep < STEPS_BEFORE_SENSOR_CONFIG)
                    return (CurrentStep + 1) + StepSeperator + TotalSteps;
                else if (CurrentStep < (STEPS_BEFORE_SENSOR_CONFIG + STEPS_AFTER_SENSOR_CONFIG + SENSOR_CONFIG_STEPS) - 1) // For zero-based index
                    return (STEPS_BEFORE_SENSOR_CONFIG + ((_currentSensorIndex) * SENSOR_CONFIG_STEPS + (CurrentStep - (STEPS_BEFORE_SENSOR_CONFIG - 1)))) + StepSeperator + TotalSteps;
                else
                    return TotalSteps + StepSeperator + TotalSteps;
            }
        }

        /// <summary>
        /// Gets the total number of steps the user will be presented with in this wizard, based on the number of sensors in the dataset.
        /// </summary>
        private int TotalSteps
        {
            get { return STEPS_BEFORE_SENSOR_CONFIG + (Sensors.Count * SENSOR_CONFIG_STEPS) + STEPS_AFTER_SENSOR_CONFIG; }
        }

        public List<Sensor> Sensors
        {
            get { return _ds.Sensors; }
        }

        public Dataset Dataset
        {
            get { return _ds; }
            set { _ds = value; NotifyOfPropertyChange(() => Sensors); }
        }


        public string Title
        {
            get { return "Import Wizard"; }
        }

        public string WizardTitle
        {
            get { return "Fix imported data" + ((SelectedSensor == null) ? "" : " for " + SelectedSensor.Name); }
        }

        public bool CanGoBack
        {
            get { return CurrentStep > 0; }
        }

        public bool CanGoForward
        {
            get { return CurrentStep < (STEPS_BEFORE_SENSOR_CONFIG + STEPS_AFTER_SENSOR_CONFIG + SENSOR_CONFIG_STEPS) - 1; } // Zero-based index
        }

        public List<Grid> SensorsToRename
        {
            get
            {
                var list = new List<Grid>();

                for (var i = 0; i < Sensors.Count; i++)
                {
                    var item = new Grid
                    {
                        Background =
                            i % 2 == 0
                                ? new SolidColorBrush(Color.FromArgb(180, 240, 240, 240))
                                : Brushes.White
                    };
                    item.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    item.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    var textbox = new TextBox
                    {
                        Text = Sensors[i].GuessConventionalNameForSensor() + "_",
                        Background = Brushes.Transparent
                    };
                    Grid.SetColumn(textbox, 1);
                    item.Children.Add(textbox);
                    var currentSensor = Sensors[i];
                    textbox.TextChanged += (o, e) =>
                    {
                        Debug.WriteLine("Fired for " + currentSensor.Name);
                        currentSensor.Name = textbox.Text;
                    };

                    var textblock = new TextBlock { Text = Sensors[i].Name };
                    Grid.SetColumn(textblock, 0);
                    item.Children.Add(textblock);

                    list.Add(item);
                }

                return list;
            }
        }
        #endregion

        #region Wizard Data Properties
        public int ErrorRowHeight
        {
            get { return (FailingErrorVisible) ? 60 : 0; }
        }

        public bool FailingErrorVisible
        {
            get { return _errorVisible; }
            set
            {
                _errorVisible = value;

                NotifyOfPropertyChange(() => FailingErrorVisible);
                NotifyOfPropertyChange(() => ErrorRowHeight);
            }
        }

        public Sensor SelectedSensor
        {
            get { return _selectedSensor; }
            set
            {
                _selectedSensor = value;

                NotifyOfPropertyChange(() => Name);
                NotifyOfPropertyChange(() => Description);
                NotifyOfPropertyChange(() => LowerLimit);
                NotifyOfPropertyChange(() => UpperLimit);
                NotifyOfPropertyChange(() => Unit);
                NotifyOfPropertyChange(() => MaximumRateOfChange);
                NotifyOfPropertyChange(() => Manufacturer);
                NotifyOfPropertyChange(() => SerialNumber);
                NotifyOfPropertyChange(() => ErrorThreshold);
                NotifyOfPropertyChange(() => SelectedSensor);
                NotifyOfPropertyChange(() => WizardTitle);
            }
        }

        public string Name { get { return (SelectedSensor == null) ? string.Empty : SelectedSensor.Name; } set { if (SelectedSensor != null) SelectedSensor.Name = value; } }

        public string Description { get { return (SelectedSensor == null) ? string.Empty : SelectedSensor.Description; } set { if (SelectedSensor != null) SelectedSensor.Description = value; } }

        public string LowerLimit
        {
            get { return (SelectedSensor == null) ? string.Empty : SelectedSensor.LowerLimit.ToString(); }
            set
            {
                float val;

                if (SelectedSensor != null && float.TryParse(value, out val))
                    SelectedSensor.LowerLimit = val;
            }
        }

        public string UpperLimit
        {
            get { return (SelectedSensor == null) ? string.Empty : SelectedSensor.UpperLimit.ToString(); }
            set
            {
                float val;

                if (SelectedSensor != null && float.TryParse(value, out val))
                    SelectedSensor.UpperLimit = val;
            }
        }

        public string Unit { get { return (SelectedSensor == null) ? string.Empty : SelectedSensor.Unit; } set { if (SelectedSensor != null) SelectedSensor.Unit = value; } }

        public string MaximumRateOfChange
        {
            get { return (SelectedSensor == null) ? string.Empty : SelectedSensor.MaxRateOfChange.ToString(); }
            set
            {
                float val;

                if (SelectedSensor != null && float.TryParse(value, out val))
                    SelectedSensor.MaxRateOfChange = val;
            }
        }

        public string Manufacturer { get { return (SelectedSensor == null) ? string.Empty : SelectedSensor.Manufacturer; } set { if (SelectedSensor != null) SelectedSensor.Manufacturer = value; } }

        public string SerialNumber { get { return (SelectedSensor == null) ? string.Empty : SelectedSensor.SerialNumber; } set { if (SelectedSensor != null) SelectedSensor.SerialNumber = value; } }

        public string ErrorThreshold
        {
            get { return (SelectedSensor == null) ? string.Empty : SelectedSensor.ErrorThreshold.ToString(); }
            set
            {
                int val;

                if (SelectedSensor != null && int.TryParse(value, out val))
                    SelectedSensor.ErrorThreshold = val;
            }
        }

        public int SummaryType
        {
            get { return (SelectedSensor == null) ? 0 : (int)SelectedSensor.SummaryType; }
            set
            {
                if (SelectedSensor != null)
                    SelectedSensor.SummaryType = (SummaryType)value;
                NotifyOfPropertyChange(() => SummaryType);
            }
        }

        public string[] SummaryTypes { get { return new[] { "Average", "Sum" }; } }

        private List<SensorTemplate> _templates;

        public List<SensorTemplate> Templates
        {
            get { return _templates ?? (_templates = SensorTemplate.ImportAll()); }
            set
            {
                _templates = value;

                foreach (var sensor in Sensors)
                    foreach (var template in Templates.Where(template => template.Matches(sensor)))
                        template.ProvideDefaultValues(sensor);
            }
        }

        #endregion

        #region Site Properties Event Handlers
        public void btnSiteCreate()
        {
            CreateEditDeleteVisible = Visibility.Collapsed;
            DoneCancelVisible = Visibility.Visible;
            SiteControlsEnabled = true;

            SelectedSite = null;
        }

        public void btnSiteEdit()
        {
            CreateEditDeleteVisible = Visibility.Collapsed;
            DoneCancelVisible = Visibility.Visible;
            SiteControlsEnabled = true;
        }

        public void btnSiteDelete()
        {
            if (SelectedSite != null)
            {
                if (Common.Confirm("Confirm Delete", "Are you sure you want to delete this site?"))
                {
                    EventLogger.LogInfo(GetType().ToString(), "Site deleted. Site name: " + SelectedSite.Name);

                    var allSites = AllSites;
                    allSites.Remove(SelectedSite);

                    AllSites = allSites;
                    SelectedSite = null;

                    Site.ExportAll(AllSites);

                    Common.ShowMessageBox("Site Management", "Site successfully removed.", false, false);
                }
            }
        }

        public void btnSiteDone()
        {
            //TODO:Live sanity checking on fields
            try
            {
                //If saving an existing Site
                if (SelectedSite != null)
                {
                    SelectedSite.GpsLocation = GPSCoords.Parse(Latitude, Longitude);

                    SelectedSite.Owner = Owner;
                    SelectedSite.PrimaryContact = PrimaryContact;
                    SelectedSite.SecondaryContact = SecondaryContact;
                    SelectedSite.Name = SiteName;
                    SelectedSite.UniversityContact = UniversityContact;
                    SelectedSite.Images = _siteImages.ToList();
                    EventLogger.LogInfo(GetType().ToString(), "Site saved. Site name: " + SelectedSite.Name);
                }
                //else if creating a new one
                else
                {
                    Site b = new Site(Site.NextID, SiteName, Owner, PrimaryContact, SecondaryContact, UniversityContact, GPSCoords.Parse(Latitude, Longitude));
                    b.Images = _siteImages.ToList();
                    _allSites.Add(b);
                    Site.ExportAll(_allSites);
                    SelectedSite = b;
                    EventLogger.LogInfo(GetType().ToString(), "Site created. Site name: " + SelectedSite.Name);
                }

                Site.ExportAll(_allSites);

                CreateEditDeleteVisible = Visibility.Visible;
                DoneCancelVisible = Visibility.Collapsed;
                SiteControlsEnabled = false;
            }
            catch (Exception e)
            {
                Common.ShowMessageBox("Error", e.Message, false, true);
                EventLogger.LogError(GetType().ToString(), "Tried to create site but failed. Details: " + e.Message);
            }
        }

        public void btnSiteCancel()
        {
            SelectedSite = SelectedSite;

            CreateEditDeleteVisible = Visibility.Visible;
            DoneCancelVisible = Visibility.Collapsed;
            SiteControlsEnabled = false;
        }

        public void btnNewImage()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Image Files|*.jpeg;*.jpg;*.png;*.gif;*.bmp|" +
                "JPEG Files (*.jpeg)|*.jpeg|JPG Files (*.jpg)|*.jpg|PNG Files (*.png)|*.png|GIF Files (*.gif)|*.gif|Bitmap Files|*.bmp";
            dlg.Title = "Select images to add";
            dlg.Multiselect = true;

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                InsertImagesForSite(dlg.FileNames);
            }
        }

        public void InsertImagesForSite(string[] fileNames)
        {
            if (_siteImages == null)
                _siteImages = new List<NamedBitmap>();

            foreach (string fileName in fileNames)
            {
                if (File.Exists(fileName))
                {
                    NamedBitmap img = new NamedBitmap(new Bitmap(fileName), Path.GetFileName(fileName));
                    _siteImages.Add(img);
                }
            }

            NotifyOfPropertyChange(() => SiteImages);
        }

        public void btnDeleteImage()
        {
            //Confirm really needed? User can just hit the cancel edit button to revert all changes
            if (SelectedImage != -1)// && Common.ShowMessageBox("Confirm Delete","Are you sure you wish to delete this image?",true,false))
            {
                _siteImages.RemoveAt(SelectedImage);
                NotifyOfPropertyChange(() => SiteImages);
            }
        }
        #endregion

        #region Contact Event Handlers
        public void btnNewPrimary()
        {
            NewContact();
        }

        public void btnNewSecondary()
        {
            NewContact();
        }

        public void btnNewUni()
        {
            NewContact();
        }

        public void btnEditPrimary()
        {
            EditContact(PrimaryContact);
        }

        public void btnEditSecondary()
        {
            EditContact(SecondaryContact);
        }

        public void btnEditUni()
        {
            EditContact(UniversityContact);
        }

        public void btnDelPrimary()
        {
            DeleteContact(PrimaryContact);
        }

        public void btnDelSecondary()
        {
            DeleteContact(SecondaryContact);
        }

        public void btnDelUni()
        {
            DeleteContact(UniversityContact);
        }

        private void DeleteContact(Contact c)
        {
            if (Common.Confirm("Confirm Delete", "Are you sure you want to delete this contact?"))
            {
                if (c != null)
                {
                    var allContacts = AllContacts;
                    allContacts.Remove(c);

                    AllContacts = allContacts;
                    c = null;

                    Contact.ExportAll(AllContacts);

                    Common.ShowMessageBox("Success", "Contact successfully removed.", false, false);
                }
            }
        }

        private void EditContact(Contact c)
        {
            var editor =
                _container.GetInstance(typeof(ContactEditorViewModel), "ContactEditorViewModel") as
                ContactEditorViewModel;

            editor.Contact = c;
            editor.AllContacts = AllContacts;

            _windowManager.ShowDialog(editor);
        }

        private void NewContact()
        {
            var editor =
                _container.GetInstance(typeof(ContactEditorViewModel), "ContactEditorViewModel") as
                ContactEditorViewModel;

            editor.Contact = null;
            editor.AllContacts = AllContacts;

            _windowManager.ShowDialog(editor);
        }
        #endregion

        #region Site Image Drag/Drop
        public bool CanDragDropImages
        {
            get { return DoneCancelVisible == Visibility.Visible; }
        }

        public void StartImageDrag(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = System.Windows.DragDropEffects.Copy;
            else
                e.Effects = System.Windows.DragDropEffects.None;
        }

        public void DoImageDrag(DragEventArgs e)
        {
            var data = (string[])e.Data.GetData(DataFormats.FileDrop);

            var acceptedFiles = new List<string>();
            var acceptedExtensions = new List<string> { ".jpeg", ".jpg", ".png", ".gif", ".bmp" };

            foreach (string file in data)
                if (acceptedExtensions.Contains(Path.GetExtension(file).ToLower()))
                    acceptedFiles.Add(file);

            InsertImagesForSite(acceptedFiles.ToArray());
        }
        #endregion

        #region Wizard Event Handlers
        public void BtnPresets()
        {
            var v = (SensorTemplateManagerViewModel)_container.GetInstance(typeof(SensorTemplateManagerViewModel), "SensorTemplateManagerViewModel");
            v.Sensors = _ds.Sensors.Select(s => new ListedSensor(s, _ds)).ToList();
            v.Dataset = Dataset;
            v.Deactivated += (o, e) =>
            {
                Templates = SensorTemplate.ImportAll(); /* Update sensor templates after potential change */
            };
            _windowManager.ShowDialog(v);
        }

        public void BtnNext()
        {
            if (CurrentStep == (STEPS_BEFORE_SENSOR_CONFIG - 1)) // Going to the first sensor configuration step
            {
                if (Sensors.Count == 0) // No sensors? Just finish up
                    CurrentStep = STEPS_BEFORE_SENSOR_CONFIG + SENSOR_CONFIG_STEPS + STEPS_AFTER_SENSOR_CONFIG - 1;
                else
                {
                    _currentSensorIndex = 0;
                    SelectedSensor = Sensors[0];
                    CurrentStep = STEPS_BEFORE_SENSOR_CONFIG;
                }
            }
            else if (CurrentStep == STEPS_BEFORE_SENSOR_CONFIG + SENSOR_CONFIG_STEPS - 1)
            {
                // Configure the next sensor
                if (_currentSensorIndex == (Sensors.Count - 1)) // Configured all the sensors
                {
                    CurrentStep = STEPS_BEFORE_SENSOR_CONFIG + SENSOR_CONFIG_STEPS + STEPS_AFTER_SENSOR_CONFIG - 1;
                    SelectedSensor = null;
                }
                else
                {
                    SelectedSensor = Sensors[++_currentSensorIndex];
                    CurrentStep = STEPS_BEFORE_SENSOR_CONFIG;
                }
            }
            else // Configuring the next stage of the current sensor, or at the start and moving through initial pages
            {
                CurrentStep++;
            }
        }

        public void BtnBack()
        {
            if (CurrentStep == (STEPS_BEFORE_SENSOR_CONFIG + SENSOR_CONFIG_STEPS + STEPS_AFTER_SENSOR_CONFIG - 1)) // Was at the last stage
            {
                if (Sensors.Count == 0)
                {
                    CurrentStep = STEPS_BEFORE_SENSOR_CONFIG - 1;
                }
                else
                {
                    _currentSensorIndex = Sensors.Count - 1;
                    SelectedSensor = Sensors[_currentSensorIndex];
                    CurrentStep--;
                }
            }
            else if (CurrentStep == (STEPS_BEFORE_SENSOR_CONFIG)) // Moving to configuring the previous sensor
            {
                if (_currentSensorIndex < 1) // At the first sensor, go to initial stages
                {
                    SelectedSensor = null;
                    CurrentStep = STEPS_BEFORE_SENSOR_CONFIG - 1;
                }
                else // Otherwise, get the previous sensor
                {
                    SelectedSensor = Sensors[--_currentSensorIndex];
                    CurrentStep = STEPS_BEFORE_SENSOR_CONFIG + SENSOR_CONFIG_STEPS - 1;
                }
            }
            else
            {
                CurrentStep--;
            }
        }

        public void BtnFinish()
        {
            TryClose();
        }

        public void BtnStdDev()
        {
            var erroneousValuesView =
                (_container.GetInstance(typeof(ErroneousValuesDetectionViewModel), "ErroneousValuesDetectionViewModel")
                 as ErroneousValuesDetectionViewModel);
            if (erroneousValuesView == null)
                return;
            erroneousValuesView.SingleSensorToUse = SelectedSensor;
            erroneousValuesView.OnlyUseStandarDeviation();
            _windowManager.ShowWindow(erroneousValuesView);
        }

        public void BtnMissingValues()
        {
            var erroneousValuesView =
                (_container.GetInstance(typeof(ErroneousValuesDetectionViewModel), "ErroneousValuesDetectionViewModel")
                 as ErroneousValuesDetectionViewModel);
            if (erroneousValuesView == null)
                return;
            erroneousValuesView.SingleSensorToUse = SelectedSensor;
            erroneousValuesView.OnlyUseMissingValues();
            _windowManager.ShowWindow(erroneousValuesView);
        }

        public void BtnOutliers()
        {
            var erroneousValuesView =
                (_container.GetInstance(typeof(ErroneousValuesDetectionViewModel), "ErroneousValuesDetectionViewModel")
                 as ErroneousValuesDetectionViewModel);
            if (erroneousValuesView == null)
                return;
            erroneousValuesView.SingleSensorToUse = SelectedSensor;
            erroneousValuesView.OnlyUseMinMaxRateOfChange();
            _windowManager.ShowWindow(erroneousValuesView);
        }

        public void BtnCalibrate()
        {
            var calibrateView = (_container.GetInstance(typeof(CalibrateSensorsViewModel), "CalibrateSensorsViewModel") as CalibrateSensorsViewModel);
            if (calibrateView == null)
                return;
            calibrateView.Dataset = _ds;
            _windowManager.ShowWindow(calibrateView);
        }

        #endregion
    }
}
