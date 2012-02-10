using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace IndiaTango.Models
{
    /// <summary>
    /// Helper class to retreive the existing manufacturers
    /// </summary>
    public static class ManufacturerHelper
    {
        public static string FileLocation
        {
            get { return Path.Combine(Common.AppDataPath, "Manufacturers.csv"); }
        }

        private static ObservableCollection<string> _manufacturers;
        public static ObservableCollection<string> Manufacturers
        {
            get
            {
                if (_manufacturers == null)
                    LoadManufacturers();
                return _manufacturers;
            }
        }

        public static void Add(string manufacturer)
        {
            if(!_manufacturers.Contains(manufacturer))
                _manufacturers.Add(manufacturer);
            SaveManufacturers();
        }

        public static void LoadManufacturers()
        {
            if(!File.Exists(FileLocation))
                _manufacturers = new ObservableCollection<string>();
            else
            {
                var manufacturers = new List<string>();
                var file = File.ReadAllText(FileLocation, Encoding.UTF8);
                manufacturers.AddRange(file.Split(','));
                _manufacturers = new ObservableCollection<string>(manufacturers);
            }
        }

        public static void SaveManufacturers()
        {
            using (var fileStream = File.CreateText(FileLocation))
            {
                for(var i = 0; i < _manufacturers.Count; i++)
                {
                    if(i > 0)
                        fileStream.Write(',');
                    fileStream.Write(_manufacturers[i]);
                }
            }
        }
    }
}
