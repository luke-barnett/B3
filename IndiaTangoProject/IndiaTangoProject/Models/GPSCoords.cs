using System;

namespace IndiaTangoProject.Models
{
    public class GPSCoords
    {
        private float _latitude;
        private float _longitude;

        public GPSCoords(float latitude, float longitude)
        {
            _latitude = latitude;
            _longitude = longitude;
        }

        public float DecimalDegreesLatitude { get { return _latitude; } set { _latitude = value; } }

        public float DecimalDegreesLongitude { get { return _longitude; } set { _longitude = value; } }
    }
}