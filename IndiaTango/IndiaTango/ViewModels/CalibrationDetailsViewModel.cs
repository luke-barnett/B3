using System;
using System.Collections.Generic;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    class CalibrationDetailsViewModel : BaseViewModel
    {
        private Sensor _sensor;
        private DateTime _timestamp = DateTime.Now;
        private string _preCalibrationPoint1 = "0";
        private string _preCalibrationPoint2 = "0";
        private string _preCalibrationPoint3 = "0";
        private string _postCalibrationPoint1 = "0";
        private string _postCalibrationPoint2 = "0";
        private string _postCalibrationPoint3 = "0";

        public Sensor Sensor
        {
            get { return _sensor; }
            set
            {
                _sensor = value;
                NotifyOfPropertyChange(() => Sensor);
                NotifyOfPropertyChange(() => Title);
            }
        }

        public List<Calibration> Calibrations
        {
            get { return _sensor.Calibrations; }
        }

        public string Title
        {
            get { return Sensor != null ? string.Format("Calibrations for {0}", Sensor.Name) : ""; }
        }

        public void AddCalibration(DateTime timestamp, float prePoint1, float prePoint2, float prePoint3, float postPoint1, float postPoint2, float postPoint3)
        {
            Sensor.Calibrations.Add(new Calibration(timestamp, prePoint1, prePoint2, prePoint3, postPoint1, postPoint2, postPoint3));
            Sensor.Calibrations = new List<Calibration>(Calibrations);
            NotifyOfPropertyChange(() => Calibrations);
        }

        public void RemoveCalibration(Calibration toRemove)
        {
            if (Sensor.Calibrations.Contains(toRemove))
                Sensor.Calibrations.Remove(toRemove);

            Sensor.Calibrations = new List<Calibration>(Calibrations);
            NotifyOfPropertyChange(() => Calibrations);
        }

        public DateTime Timestamp
        {
            get { return _timestamp; }
            set
            {
                _timestamp = value;
                NotifyOfPropertyChange(() => Timestamp);
            }
        }

        public string PreCalibrationPoint1
        {
            get { return _preCalibrationPoint1; }
            set
            {
                _preCalibrationPoint1 = value;
                NotifyOfPropertyChange(() => PreCalibrationPoint1);
            }
        }

        public string PreCalibrationPoint2
        {
            get { return _preCalibrationPoint2; }
            set
            {
                _preCalibrationPoint2 = value;
                NotifyOfPropertyChange(() => PreCalibrationPoint2);
            }
        }

        public string PreCalibrationPoint3
        {
            get { return _preCalibrationPoint3; }
            set
            {
                _preCalibrationPoint3 = value;
                NotifyOfPropertyChange(() => PreCalibrationPoint3);
            }
        }

        public string PostCalibrationPoint1
        {
            get { return _postCalibrationPoint1; }
            set
            {
                _postCalibrationPoint1 = value;
                NotifyOfPropertyChange(() => PostCalibrationPoint1);
            }
        }

        public string PostCalibrationPoint2
        {
            get { return _postCalibrationPoint2; }
            set
            {
                _postCalibrationPoint2 = value;
                NotifyOfPropertyChange(() => PostCalibrationPoint2);
            }
        }

        public string PostCalibrationPoint3
        {
            get { return _postCalibrationPoint3; }
            set
            {
                _postCalibrationPoint3 = value;
                NotifyOfPropertyChange(() => PostCalibrationPoint3);
            }
        }

        public void Add()
        {
            try
            {
                AddCalibration(Timestamp, float.Parse(PreCalibrationPoint1), float.Parse(PreCalibrationPoint2), float.Parse(PreCalibrationPoint3), float.Parse(PostCalibrationPoint1), float.Parse(PostCalibrationPoint2), float.Parse(PostCalibrationPoint3));
            }
            catch (Exception e)
            {
                Common.ShowMessageBoxWithException("An Error Occured", "Was unable to add calibration", false, true, e);
            }
        }
    }
}
