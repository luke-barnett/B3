using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace IndiaTango.Models
{
    /// <summary>
    /// Helper class for retrieving the units
    /// </summary>
    public static class UnitsHelper
    {
        public static string FileLocation
        {
            get { return Path.Combine(Common.AppDataPath, "Units.csv"); }
        }

        private static ObservableCollection<string> _units;
        /// <summary>
        /// The collection of units
        /// </summary>
        public static ObservableCollection<string> Units
        {
            get
            {
                if (_units == null)
                    LoadUnits();
                return _units;
            }
        }

        public static void Add(string unit)
        {
            if (!_units.Contains(unit))
                _units.Add(unit);
            SaveUnits();
        }

        private static void LoadUnits()
        {
            if (!File.Exists(FileLocation))
            {
                _units = GenerateUnits();
                SaveUnits();
            }
            else
            {
                var units = new List<string>();
                var file = File.ReadAllText(FileLocation, Encoding.UTF8);
                units.AddRange(file.Split(','));
                _units = new ObservableCollection<string>(units);
            }
        }

        private static void SaveUnits()
        {
            using (var fileStream = File.CreateText(FileLocation))
            {
                for (var i = 0; i < _units.Count; i++)
                {
                    if (i > 0)
                        fileStream.Write(',');
                    fileStream.Write(_units[i]);
                }
            }
        }

        private static ObservableCollection<string> GenerateUnits()
        {
            var units = new List<string>();

            var unitsFile = Path.Combine(Assembly.GetExecutingAssembly().Location.Replace("B3.exe", ""), "Resources", "units.csv");

            if (File.Exists(unitsFile))
            {
                units.AddRange(File.ReadAllText(unitsFile, Encoding.UTF8).Split(','));
                units = units.Distinct().ToList();
                units.Sort();
            }

            return new ObservableCollection<string>(units);
        }
    }
}
