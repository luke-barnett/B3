using System;
using ProtoBuf;

namespace IndiaTango.Models
{
    /// <summary>
    /// Represents a pair of GPS co-ordinates, optionally constructed using DMS (Degrees Minutes Seconds) or Decimal Degrees notation.
    /// </summary>
    [Serializable]
    [ProtoContract]
    public class GPSCoords
    {
        private decimal _latitude;
        private decimal _longitude;

        private GPSCoords() {} // Necessary for serialisation.

        /// <summary>
        /// Creates a GPSCoords object using Decimal Degrees values.
        /// </summary>
        /// <param name="latitude">Latitude in Decimal Degrees.</param>
        /// <param name="longitude">Longitude in Decimal Degrees.</param>
        public GPSCoords(decimal latitude, decimal longitude)
        {
            _latitude = latitude;
            _longitude = longitude;
        }

        /// <summary>
        /// Creates a GPSCoords object using Degrees Minutes Seconds (DMS) values.
        /// </summary>
        /// <param name="latitude">Latitude in DMS notation.</param>
        /// <param name="longitude">Longitude in DMS notation.</param>
        public GPSCoords(string latitude, string longitude)
        {
            _latitude = ConvertDMSToDecimalDegrees(latitude);
            _longitude = ConvertDMSToDecimalDegrees(longitude);
        }

        #region Private Methods
        /// <summary>
        /// Converts Degrees Minutes Seconds (DMS) values to Decimal Degrees notation.
        /// </summary>
        /// <param name="coordinate">The DMS value to convert.</param>
        /// <returns>The resulting decimal degrees value.</returns>
        private decimal ConvertDMSToDecimalDegrees(string coordinate)
        {
        	string[] components = coordinate.Split(' ');

            if (components.Length == 3)
            {
                decimal degrees = decimal.Parse(components[0].Substring(1));

                decimal minutes = decimal.Parse(components[1]) / 60;

                decimal seconds = decimal.Parse(components[2]) / (60 * 60);

                return coordinate.StartsWith("S") || coordinate.StartsWith("W")
                           ? (degrees + minutes + seconds) * -1
                           : degrees + minutes + seconds;
            }
            else
                return 0;
        }

        /// <summary>
        /// Converts Decimal Degrees values to DMS (Degrees Minutes Seconds) notation.
        /// </summary>
        /// <param name="coordinate">The decimal degrees values to convert.</param>
        /// <param name="latitude">Whether or not the given value is latitude.</param>
        /// <returns>The resulting Degrees Minutes Seconds (DMS) value.</returns>
        private string ConvertDecimalDegreesToDMS(decimal coordinate, bool latitude)
        {
        	string direction;

        	if (latitude)
                direction = (coordinate >= 0) ? "N" : "S";
            else
                direction = (coordinate >= 0) ? "E" : "W";

            coordinate = Math.Abs(coordinate);

            decimal degrees = Decimal.Truncate(coordinate);
            decimal minutes = Decimal.Truncate((coordinate - degrees) * 60);
            decimal seconds = Decimal.Truncate((((coordinate - degrees) * 60) - minutes) * 60);

            return String.Format("{0}{1} {2} {3}", direction, degrees.ToString(), minutes.ToString(), seconds.ToString());
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets or sets the latitude value, using decimal degrees.
        /// </summary>
        [ProtoMember(1)]
        public decimal DecimalDegreesLatitude { get { return _latitude; } set { _latitude = value; } }

        /// <summary>
        /// Gets or sets the longitude value, using decimal degrees.
        /// </summary>
        [ProtoMember(2)]
        public decimal DecimalDegreesLongitude { get { return _longitude; } set { _longitude = value; } }

        /// <summary>
        /// Gets or sets the latitude value, using Degrees Minutes Seconds (DMS).
        /// </summary>
        public string DMSLatitude { get { return ConvertDecimalDegreesToDMS(_latitude, true); } set { _latitude = ConvertDMSToDecimalDegrees(value); } }

        /// <summary>
        /// Gets or sets the longitude value, using Degrees Minutes Seconds (DMS).
        /// </summary>
        public string DMSLongitude { get { return ConvertDecimalDegreesToDMS(_longitude, false); } set { _longitude = ConvertDMSToDecimalDegrees(value); } }
        #endregion

        public override bool Equals(object obj)
        {
            return (obj is GPSCoords) && (obj as GPSCoords).DecimalDegreesLatitude == DecimalDegreesLatitude &&
                   (obj as GPSCoords).DecimalDegreesLongitude == DecimalDegreesLongitude;
        }

        public override string ToString()
        {
            return String.Format("Lat [{0}] Long [{1}]", _latitude, _longitude);
        }

        public static GPSCoords Parse(string latitude, string longitude)
        {
            decimal lat;
            decimal lng;

            if (decimal.TryParse(latitude, out lat) && decimal.TryParse(longitude, out lng))
                return new GPSCoords(lat, lng);
            else
                return new GPSCoords(latitude, longitude);
        }
    }
}