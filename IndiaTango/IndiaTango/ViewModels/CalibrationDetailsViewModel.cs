using System;
using System.Collections.Generic;
using IndiaTango.Models;

namespace IndiaTango.ViewModels
{
    class CalibrationDetailsViewModel : BaseViewModel
    {
        private Sensor _sensor;
        private DateTime _timestamp = DateTime.Now;
        private string _preOffset;
        private string _preSpan;
        private string _postOffset;
        private string _postSpan;

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

        public void AddCalibration(DateTime timestamp, float preOffset, float preSpan, float postOffset, float postSpan)
        {
            Sensor.Calibrations.Add(new Calibration(timestamp, preSpan, preOffset, postSpan, postOffset));
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

        public string PreOffset
        {
            get { return _preOffset; }
            set
            {
                _preOffset = value;
                NotifyOfPropertyChange(() => PreOffset);
            }
        }

        public string PreSpan
        {
            get { return _preSpan; }
            set
            {
                _preSpan = value;
                NotifyOfPropertyChange(() => PreSpan);
            }
        }

        public string PostOffset
        {
            get { return _postOffset; }
            set
            {
                _postOffset = value;
                NotifyOfPropertyChange(() => PostOffset);
            }
        }

        public string PostSpan
        {
            get { return _postSpan; }
            set
            {
                _postSpan = value;
                NotifyOfPropertyChange(() => PostSpan);
            }
        }

        public void Add()
        {
            try
            {
                AddCalibration(Timestamp, float.Parse(PreOffset), float.Parse(PreSpan), float.Parse(PostOffset), float.Parse(PostSpan));
            }
            catch (Exception e)
            {
                Common.ShowMessageBoxWithException("An Error Occured", "Was unable to add calibration", false, true, e);
            }
        }
    }
}
