using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Caliburn.Micro;
using IndiaTango.Models;
using System.Windows.Controls;
using Cursors = System.Windows.Input.Cursors;
using Image = System.Windows.Controls.Image;
using Orientation = System.Windows.Controls.Orientation;
using DragEventArgs = System.Windows.DragEventArgs;
using DataFormats = System.Windows.DataFormats;

namespace IndiaTango.ViewModels
{
    class SessionViewModel : BaseViewModel
    {
        #region Private Members
        private readonly SimpleContainer _container;
        private readonly IWindowManager _windowManager;
        private Dataset _ds;
        private BackgroundWorker _bw;

        private static double _progressBarPercent;

        private const string StatusTextAwaitingInput =
            "To Start, click Import Data, then fill out the properties to the right.";

        private string _statusLabelText = StatusTextAwaitingInput;
        private Visibility _progressBarVisible = Visibility.Hidden;
        private bool _actionButtonsEnabled;
        private bool _siteControlsEnabled;
        private bool _saveButtonEnabled;
        private Visibility _createEditDeleteVisible = Visibility.Visible;
        private Visibility _doneCancelVisible = Visibility.Hidden;

        private Contact _primaryContact;
        private Contact _secondaryContact;
        private Contact _universityContact;
        private ObservableCollection<Site> _allSites = new ObservableCollection<Site>();
        private ObservableCollection<Contact> _allContacts = new ObservableCollection<Contact>();
        private List<NamedBitmap> _siteImages = new List<NamedBitmap>();
        private int _selectedImage = -1;

        #endregion

        public SessionViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
            _ds = new Dataset(null);
            _allSites = Site.ImportAll();
            _allContacts = Contact.ImportAll();

            //Hack used to force the damn buttons to update
            DoneCancelVisible = Visibility.Visible;
            DoneCancelVisible = Visibility.Collapsed;
        }

        /// <summary>
        /// Sets the dataset to use for the session
        /// </summary>
        public Dataset Dataset
        {
            set
            {
                _ds = value;
                ActionButtonsEnabled = true;
                SaveButtonEnabled = true;
                //Sigh
                if (_ds.Site == null)
                    return;
                SiteName = _ds.Site.Name;
                Owner = _ds.Site.Owner;
                Latitude = _ds.Site.GpsLocation.DecimalDegreesLatitude.ToString();
                Longitude = _ds.Site.GpsLocation.DecimalDegreesLongitude.ToString();
                PrimaryContact = _ds.Site.PrimaryContact;
                SecondaryContact = _ds.Site.SecondaryContact;
                UniversityContact = _ds.Site.UniversityContact;

                StatusLabelText = "";

                /*NotifyOfPropertyChange(() => SelectedSite);
                NotifyOfPropertyChange(() => SiteName);
                NotifyOfPropertyChange(() => Owner);
                NotifyOfPropertyChange(() => Latitude);
                NotifyOfPropertyChange(() => Longitude);
                NotifyOfPropertyChange(() => PrimaryContact);
                NotifyOfPropertyChange(() => SecondaryContact);
                NotifyOfPropertyChange(() => UniversityContact);
                NotifyOfPropertyChange(() => EditDeleteEnabled);*/
            }
        }

        #region View Properties
        //TODO: Make a gloabl 'editing/creating/viewing site' state that the properties reference
        private Visibility _sensorWarningVis = Visibility.Collapsed;

        public int SelectedImage
        {
            get { return _selectedImage; }
            set { _selectedImage = value; NotifyOfPropertyChange(() => SelectedImage); }
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


        public Visibility SensorWarningVisible
        {
            get { return _sensorWarningVis; }
            set { _sensorWarningVis = value; NotifyOfPropertyChange(() => SensorWarningVisible); }
        }

        public string Title
        {
            get { return string.Format("{0}", (_ds != null ? _ds.IdentifiableName : Common.UnknownSite)); }
        }

        public double ProgressBarPercent
        {
            get { return _progressBarPercent; }
            set
            {
                _progressBarPercent = value;
                StatusLabelText = LoadingProgressString;

                NotifyOfPropertyChange(() => ProgressBarPercent);
                NotifyOfPropertyChange(() => ProgressBarPercentDouble);
            }
        }

        public double ProgressBarPercentDouble
        {
            get { return ProgressBarPercent / 100; }
        }

        public string ProgressState { get { return ActionButtonsEnabled ? "None" : "Normal"; } }

        public string StatusLabelText
        {
            get { return _statusLabelText; }
            set
            {
                _statusLabelText = value;
                NotifyOfPropertyChange(() => StatusLabelText);
            }
        }

        public Visibility ProgressBarVisible
        {
            get { return _progressBarVisible; }
            set
            {
                _progressBarVisible = value;
                NotifyOfPropertyChange(() => ProgressBarVisible);
            }
        }

        public bool ActionButtonsEnabled
        {
            get { return _actionButtonsEnabled; }
            set
            {
                _actionButtonsEnabled = value;
                NotifyOfPropertyChange(() => ActionButtonsEnabled);
                NotifyOfPropertyChange(() => ProgressState);
            }
        }

        public bool SaveButtonEnabled
        {
            get { return _saveButtonEnabled; }
            set
            {
                _saveButtonEnabled = value;
                NotifyOfPropertyChange(() => SaveButtonEnabled);
            }
        }

        public bool EditDeleteEnabled
        {
            get { return SelectedSite != null; }
        }

        public bool SiteListEnabled
        {
            get { return DoneCancelVisible != Visibility.Visible; }
        }

        public string LoadingProgressString { get { return string.Format("Loading data: {0}%", ProgressBarPercent); } }

        public List<Sensor> SensorList
        {
            get { return _ds.Sensors; }
            set
            {
                _ds.Sensors = value;
                NotifyOfPropertyChange(() => SensorList);
            }
        }

        public List<Sensor> SelectedSensor = new List<Sensor>();

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

        private bool _importEnabled = true;
        public bool ImportEnabled { get { return _importEnabled; } set { _importEnabled = value; NotifyOfPropertyChange(() => ImportEnabled); NotifyOfPropertyChange(() => ShowImportCancel); } }

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

        public Visibility ShowImportCancel
        {
            get { return (ImportEnabled) ? Visibility.Collapsed : Visibility.Visible; }
        }
        #endregion

        #region Site Properties
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

        public Site SelectedSite
        {
            get { return _ds.Site; }
            set
            {
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
        #endregion

        #region Event Handlers
        public void OnLoaded()
        {
            EventLogger.LogInfo(GetType().ToString(), "Session loaded.");
        }

        public void OnUnloaded()
        {
            EventLogger.LogInfo(GetType().ToString(), "Session closed.");
        }

        public void btnImport()
        {
            _bw = new BackgroundWorker();
            _bw.WorkerReportsProgress = true;
            _bw.WorkerSupportsCancellation = true;

            var fileDialog = new OpenFileDialog();
            fileDialog.Filter = "CSV Files|*.csv";
            var result = fileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                var askUser = _container.GetInstance(typeof(SpecifyValueViewModel), "SpecifyValueViewModel") as SpecifyValueViewModel;

                if (askUser == null)
                {
                    Common.ShowMessageBox("EPIC FAIL", "RUN AROUND WITH NO REASON", false, true);
                    return;
                }

                askUser.ComboBoxItems = new List<string> { "Keep old values", "Keep new values" };
                askUser.Text = "Keep old values";
                askUser.ShowComboBox = true;
                askUser.Message = "How do you want to handle overlapping points";
                askUser.CanEditComboBox = false;
                askUser.Title = "Importing";

                if (_ds.Sensors.Count != 0)
                    _windowManager.ShowDialog(askUser);

                var keepOldValues = askUser.Text.CompareTo("Keep old values") == 0;
                Debug.Print("Keep old values {0}", keepOldValues);


                _bw.DoWork += delegate(object sender, DoWorkEventArgs eventArgs)
                {
                    EventLogger.LogInfo("BackgroundImportThread", "Data import started.");
                    ActionButtonsEnabled = false;
                    ProgressBarVisible = Visibility.Visible;

                    var reader = new CSVReader(fileDialog.FileName);

                    reader.ProgressChanged += ImportProgressChanged;

                    var readSensors = reader.ReadSensors(_bw, _ds);  // Prevent null references

                    if (readSensors == null)
                    {
                        eventArgs.Cancel = true;
                        ProgressBarPercent = 0;
                        StatusLabelText = StatusTextAwaitingInput;
                        ActionButtonsEnabled = false;
                    }
                    else
                    {
                        if (_ds.Sensors.Count == 0)
                            _ds.Sensors = readSensors;
                        else
                            AddValuesToSensors(readSensors, keepOldValues);

                        // Loaded successfully
                        ActionButtonsEnabled = true;
                        SaveButtonEnabled = true;
                        StatusLabelText = "";
                        SensorList = _ds.Sensors;

                        var sensorTemplates = SensorTemplate.ImportAll();
                        foreach (var s in readSensors)
                        {
                            foreach (var sensorTemplate in sensorTemplates)
                            {
                                sensorTemplate.ProvideDefaultValues(s);
                            }
                            if (s.IsFailing(_ds))
                            {
                                SensorWarningVisible = Visibility.Visible;
                            }
                        }
                    }

                    NotifyOfPropertyChange(() => Title);
                    ImportEnabled = true;
                    ProgressBarVisible = Visibility.Hidden;
                    EventLogger.LogInfo("BackgroundImportThread", "Data import complete.");
                };

                _bw.RunWorkerCompleted += (obj, ev) =>
                                          	{
                                          		if (ev.Cancelled)
                                          			return;

                                          		// Show the wizard every time data is imported?
                                          		EventLogger.LogInfo("UIThread", "Starting the import wizard...");
                                          		var wizard =
                                          			(WizardViewModel)
                                          			_container.GetInstance(typeof (WizardViewModel), "WizardViewModel");
                                          		wizard.Dataset = _ds;

												Console.WriteLine("selected site = " + wizard.SelectedSite);
												Console.WriteLine("ds site = " + _ds.Site);

                                          		wizard.Deactivated += (o, e) =>
                                          		                      	{
                                          		                      		EventLogger.LogInfo("WizardView", "Completed the import wizard, ending at step " + wizard.ThisStep);

																			//Update any contacts/sites that have changed
                                          		                      	    AllSites = wizard.AllSites;
                                          		                      	    AllContacts = wizard.AllContacts;

																			Console.WriteLine("selected site = " + wizard.SelectedSite);
																			Console.WriteLine("ds site = " + _ds.Site);
                                          		                      		SelectedSite = _ds.Site;
                                          		                      	};
                                          	
                                                  _windowManager.ShowDialog(wizard);
                                              };

                ImportEnabled = false;

                _bw.RunWorkerAsync();
            }
        }

        public void ImportProgressChanged(object o, ReaderProgressChangedArgs e)
        {
            ProgressBarPercent = e.Progress;
        }

        public void imgListDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("Test");
        }

        public void btnGraph()
        {
            var graphView = (_container.GetInstance(typeof(GraphViewModel), "GraphViewModel") as GraphViewModel);
            graphView.SensorList = SensorList;
            graphView.Dataset = _ds;
            _windowManager.ShowWindow(graphView);
        }

        public void btnSave()
        {
            EventLogger.LogInfo(GetType().ToString(), "Session save started.");
            var saveFileDialog = new SaveFileDialog { Filter = "Session Files|*.indiatango" };
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                var bw = new BackgroundWorker();
                bw.DoWork += (o, e) =>
                                 {
                                     using (var stream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                                         new BinaryFormatter().Serialize(stream, _ds);
                                     EventLogger.LogInfo(GetType().ToString(), string.Format("Session save complete. File saved to: {0}", saveFileDialog.FileName));
                                 };
                bw.RunWorkerCompleted += (o, e) =>
                                             {
                                                 ApplicationCursor = Cursors.Arrow;
                                                 ImportEnabled = true;
                                                 ActionButtonsEnabled = true;
                                             };
                ApplicationCursor = Cursors.Wait;
                ImportEnabled = false;
                ActionButtonsEnabled = false;
                bw.RunWorkerAsync();
            }
            else
                EventLogger.LogInfo(GetType().ToString(), "Session save aborted");

        }

        public void btnExport()
        {
            var exportWindow =
                    _container.GetInstance(typeof(ExportViewModel), "ExportViewModel") as ExportViewModel;
            exportWindow.Dataset = _ds;

            _windowManager.ShowDialog(exportWindow);
        }

        public void btnOutOfRangeValues()
        {
            var outlierView =
                (_container.GetInstance(typeof(OutlierDetectionViewModel), "OutlierDetectionViewModel") as
                 OutlierDetectionViewModel);
            outlierView.Dataset = _ds;
            _windowManager.ShowWindow(outlierView);
        }

        public void btnMissingValues()
        {
            var MissingValuesView =
                (_container.GetInstance(typeof(MissingValuesViewModel), "MissingValuesViewModel") as MissingValuesViewModel);
            MissingValuesView.Dataset = _ds;
            MissingValuesView.SensorList = SensorList;
            _windowManager.ShowWindow(MissingValuesView);
        }

        public void btnEditPoints()
        {
            Common.ShowFeatureNotImplementedMessageBox();
        }

        public void btnCalibrate()
        {
            var calibrateView = (_container.GetInstance(typeof(CalibrateSensorsViewModel), "CalibrateSensorsViewModel") as CalibrateSensorsViewModel);
            calibrateView.Dataset = _ds;
            _windowManager.ShowWindow(calibrateView);
        }

        public void btnRevert()
        {
            string changes = "The following sensors would be reverted:\n";

            foreach (var sensor in _ds.Sensors)
                if (sensor.UndoStates.Count > 1)
                    changes += sensor.Name + "\n";

            if (Common.ShowMessageBoxWithExpansion("Confirm Revert", "Are you sure you wish to revert all sensors back to their original state?\n" +
                                                                     "Modified data will still be available within the redo states for each sensor.", true, false, changes))
            {
                foreach (var sensor in _ds.Sensors)
                    sensor.RevertToRaw();
            }
        }

        public void btnErroneousValues()
        {
            var erroneousValuesView =
                (_container.GetInstance(typeof(ErroneousValuesDetectionViewModel), "ErroneousValuesDetectionViewModel")
                 as ErroneousValuesDetectionViewModel);
            if (erroneousValuesView == null)
                return;
            erroneousValuesView.DataSet = _ds;
            _windowManager.ShowWindow(erroneousValuesView);
        }

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

        public void btnSensors()
        {
            var editSensor =
                    _container.GetInstance(typeof(EditSensorViewModel), "EditSensorViewModel") as EditSensorViewModel;

            var newSensorList = new List<ListedSensor>();

            foreach (var s in SensorList)
                newSensorList.Add(new ListedSensor(s, _ds));

            editSensor.Dataset = _ds;

            _windowManager.ShowWindow(editSensor);
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

        public void btnCancel()
        {
            if (_bw != null)
            {
                try
                {
                    _bw.CancelAsync();
                    ActionButtonsEnabled = true;
                    EventLogger.LogWarning(GetType().ToString(), "Data import canceled by user.");
                }
                catch (InvalidOperationException ex)
                {
                    Common.ShowMessageBox("Error", "You cannot cancel the loading of this data set at this time. Try again later.",
                                          false, true);
                    System.Diagnostics.Debug.WriteLine("Cannot cancel data loading thread - " + ex);
                    EventLogger.LogError(GetType().ToString(), "Data import could not be canceled.");
                }
            }
        }

        public void SelectionChanged(SelectionChangedEventArgs e)
        {
            foreach (Sensor item in e.RemovedItems)
            {
                SelectedSensor.Remove(item);
            }

            foreach (Sensor item in e.AddedItems)
            {
                SelectedSensor.Add(item);
            }
        }

        #endregion

        #region Contact Add/Edit/Delete Handlers
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

        private void AddValuesToSensors(IEnumerable<Sensor> sensors, bool keepOldValues)
        {
            //For all the sensors we have imported
            foreach (var newSensor in sensors)
            {
                //Check to see if we match a current sensor
                var matchingSensor = (from sensor in _ds.Sensors where sensor.RawName.CompareTo(newSensor.RawName) == 0 select sensor).DefaultIfEmpty(null).FirstOrDefault();

                if (matchingSensor == null)
                {
                    //If we don't  then add it as a new sensor
                    _ds.Sensors.Add(newSensor);
                    EventLogger.LogInfo("Importer", "Added new sensor: " + newSensor.Name);
                }
                else
                {
                    //Otherwise clone the current state
                    var newState = matchingSensor.CurrentState.Clone();
                    //Check to see if values are inserted
                    var insertedValues = false;

                    //And add values for any new dates we want
                    foreach (var value in newSensor.CurrentState.Values.Where(value => !keepOldValues || !newState.Values.ContainsKey(value.Key)))
                    {
                        newState.Values[value.Key] = value.Value;
                        insertedValues = true;
                    }
                    //Give a reason
                    newState.Reason = "Imported new values on " + DateTime.Now;
                    if (insertedValues)
                    {
                        //Insert new state
                        matchingSensor.AddState(newState);
                        EventLogger.LogSensorInfo(matchingSensor.Name, "Added values from new import");
                    }
                    else
                        EventLogger.LogSensorInfo(matchingSensor.Name, "Matched to imported sensor but no new values found");
                }
            }
        }

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
    }
}
