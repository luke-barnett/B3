using System;

namespace IndiaTangoProject.Models
{
    public class GPSCoords
    {
        private decimal _latitude;
        private decimal _longitude;

        public GPSCoords(decimal latitude, decimal longitude)
        {
            _latitude = latitude;
            _longitude = longitude;
        }

        public GPSCoords(string latitude, string longitude)
        {
            _latitude = ConvertDMSToDecimalDegrees(latitude);
            _longitude = ConvertDMSToDecimalDegrees(longitude);
        }

        private decimal ConvertDMSToDecimalDegrees(string coordinate)
        {
            decimal degrees;
            decimal minutes;
            decimal seconds;

            string[] components = coordinate.Split(' ');

            degrees = decimal.Parse(components[0].Substring(1));

            minutes = decimal.Parse(components[1]) / 60;

            seconds = decimal.Parse(components[2]) / (60 * 60);

            return coordinate.StartsWith("S") || coordinate.StartsWith("W") ? (degrees + minutes + seconds) * -1 : degrees + minutes + seconds;
        }

        public decimal DecimalDegreesLatitude { get { return _latitude; } set { _latitude = value; } }

        public decimal DecimalDegreesLongitude { get { return _longitude; } set { _longitude = value; } }

        public string DMSLatitude { get { return "0";  } }
    }
}