using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace IndiaTango.Models
{
    /// <summary>
    /// Helper class for loading existing owners
    /// </summary>
    public class OwnerHelper
    {
        public static string FileLocation
        {
            get { return Path.Combine(Common.AppDataPath, "Owners.csv"); }
        }

        private static ObservableCollection<string> _owners;
        public static ObservableCollection<string> Owners
        {
            get
            {
                if (_owners == null)
                    LoadOwners();
                return _owners;
            }
        }

        public static void Add(string description)
        {
            if (!Owners.Contains(description))
                Owners.Add(description);
            SaveOwners();
        }

        public static void LoadOwners()
        {
            if (!File.Exists(FileLocation))
                _owners = new ObservableCollection<string>();
            else
            {
                var owners = new List<string>();
                var file = File.ReadAllText(FileLocation, Encoding.UTF8);
                owners.AddRange(file.Split(','));
                _owners = new ObservableCollection<string>(owners);
            }
        }

        public static void SaveOwners()
        {
            using (var fileStream = File.CreateText(FileLocation))
            {
                for (var i = 0; i < _owners.Count; i++)
                {
                    if (i > 0)
                        fileStream.Write(',');
                    fileStream.Write(_owners[i]);
                }
            }
        }
    }
}
