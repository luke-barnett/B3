using System;

namespace IndiaTango.Models
{
    public class GPSCoords
    {
        private decimal _latitude;
        private decimal _longitude;

        /// <summary>
        /// Creates a GPSCoords object using Decimal Degrees values
        /// </summary>
        /// <param name="latitude">latitude in decimal degrees</param>
        /// <param name="longitude">longitude in decimal degrees</param>
        public GPSCoords(decimal latitude, decimal longitude)
        {
            _latitude = latitude;
            _longitude = longitude;
        }

        /// <summary>
        /// Creates a GPSCoords object using DMS values
        /// </summary>
        /// <param name="latitude">latitude in DMS</param>
        /// <param name="longitude">longitude in DMS</param>
        public GPSCoords(string latitude, string longitude)
        {
            _latitude = ConvertDMSToDecimalDegrees(latitude);
            _longitude = ConvertDMSToDecimalDegrees(longitude);
        }

        #region private methods
        /// <summary>
        /// Converts DMS values to decimal degrees
        /// </summary>
        /// <param name="coordinate">The DMS value to convert</param>
        /// <returns>The resulting decimal degrees value</returns>
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

        /// <summary>
        /// Converts decimal degree vales to DMS
        /// </summary>
        /// <param name="coordinate">The decimal degree to convert</param>
        /// <param name="latitude">If the value is latitude or not</param>
        /// <returns>The resulting DMS value</returns>
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
        #endregion

        #region public properties
        /// <summary>
        /// Gets or sets the latitude value, using decimal degrees
        /// </summary>
        public decimal DecimalDegreesLatitude { get { return _latitude; } set { _latitude = value; } }

        /// <summary>
        /// Gets or sets the longitude value, using decimal degrees
        /// </summary>
        public decimal DecimalDegreesLongitude { get { return _longitude; } set { _longitude = value; } }

        /// <summary>
        /// Gets or sets the latitude value, using DMS
        /// </summary>
        public string DMSLatitude { get { return ConvertDecimalDegreesToDMS(_latitude, true); } set { _latitude = ConvertDMSToDecimalDegrees(value); } }

        /// <summary>
        /// Gets or sets the longitude value, using DMS
        /// </summary>
        public string DMSLongitude { get { return ConvertDecimalDegreesToDMS(_longitude, false); } set { _longitude = ConvertDMSToDecimalDegrees(value); } }
        #endregion
    }
}