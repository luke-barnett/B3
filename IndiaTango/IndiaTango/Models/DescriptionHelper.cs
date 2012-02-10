using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace IndiaTango.Models
{
    /// <summary>
    /// Helper class for descriptions
    /// </summary>
    class DescriptionHelper
    {
        public static string FileLocation
        {
            get { return Path.Combine(Common.AppDataPath, "Descriptions.csv"); }
        }

        private static ObservableCollection<string> _descriptions;
        public static ObservableCollection<string> Descriptions
        {
            get
            {
                if (_descriptions == null)
                    LoadDescriptions();
                return _descriptions;
            }
        }

        public static void Add(string description)
        {
            if (!_descriptions.Contains(description))
                _descriptions.Add(description);
            SaveDescriptions();
        }

        public static void LoadDescriptions()
        {
            if (!File.Exists(FileLocation))
                _descriptions = new ObservableCollection<string>();
            else
            {
                var descriptions = new List<string>();
                var file = File.ReadAllText(FileLocation, Encoding.UTF8);
                descriptions.AddRange(file.Split(','));
                _descriptions = new ObservableCollection<string>(descriptions);
            }
        }

        public static void SaveDescriptions()
        {
            using (var fileStream = File.CreateText(FileLocation))
            {
                for (var i = 0; i < _descriptions.Count; i++)
                {
                    if (i > 0)
                        fileStream.Write(',');
                    fileStream.Write(_descriptions[i]);
                }
            }
        }
    }
}
