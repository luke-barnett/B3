using System;

namespace IndiaTango.Models
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

            if (components.Length == 3)
            {
                degrees = decimal.Parse(components[0].Substring(1));

                minutes = decimal.Parse(components[1]) / 60;

                seconds = decimal.Parse(components[2]) / (60 * 60);

                return coordinate.StartsWith("S") || coordinate.StartsWith("W")
                           ? (degrees + minutes + seconds) * -1
                           : degrees + minutes + seconds;
            }
            else
                return 0;
        }

        private string ConvertDecimalDegreesToDMS(decimal coordinate, bool latitude)
        {
            decimal degrees;
            decimal minutes;
            decimal seconds;
            string direction = "N";

            if (latitude)
                direction = (coordinate >= 0) ? "N" : "S";
            else
                direction = (coordinate >= 0) ? "E" : "W";

            coordinate = Math.Abs(coordinate);

            degrees = Decimal.Truncate(coordinate);
            minutes = Decimal.Truncate((coordinate - degrees) * 60);
            seconds = Decimal.Truncate((((coordinate - degrees) * 60) - minutes) * 60);

            return String.Format("{0}{1} {2} {3}", direction, degrees.ToString(), minutes.ToString(), seconds.ToString());
        }

        public decimal DecimalDegreesLatitude { get { return _latitude; } set { _latitude = value; } }

        public decimal DecimalDegreesLongitude { get { return _longitude; } set { _longitude = value; } }

        public string DMSLatitude { get { return ConvertDecimalDegreesToDMS(_latitude, true); } set { _latitude = ConvertDMSToDecimalDegrees(value); } }

        public string DMSLongitude { get { return ConvertDecimalDegreesToDMS(_longitude, false); } set { _longitude = ConvertDMSToDecimalDegrees(value); } }
    }
}