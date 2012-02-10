using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace IndiaTango.Models
{
    /// <summary>
    /// Helper class for the set of sensor vocabularies available
    /// </summary>
    public static class SensorVocabulary
    {
        public static string FileLocation
        {
            get { return Path.Combine(Common.AppDataPath, "SensorVocabulary.csv"); }
        }

        private static List<string> _vocabulary;
        public static List<string> Vocabulary
        {
            get
            {
                if (_vocabulary == null)
                    LoadVocabulary();
                return _vocabulary;
            }
        }

        private static void LoadVocabulary()
        {
            if (!File.Exists(FileLocation))
            {
                _vocabulary = GenerateVocabulary();
                SaveVocabulary();
            }
            else
            {
                var vocabulary = new List<string>();
                var file = File.ReadAllText(FileLocation);
                vocabulary.AddRange(file.Split(','));
                _vocabulary = vocabulary;
            }
        }

        private static void SaveVocabulary()
        {
            using (var fileStream = File.CreateText(FileLocation))
            {
                for (var i = 0; i < _vocabulary.Count; i++)
                {
                    if (i > 0)
                        fileStream.Write(',');
                    fileStream.Write(_vocabulary[i]);
                }
            }
        }

        private static List<string> GenerateVocabulary()
        {
            var vocabulary = new List<string>();

            #region GLEON VOCABULARY

            var gleonFile = Path.Combine(Assembly.GetExecutingAssembly().Location.Replace("B3.exe", ""), "Resources", "GLEON_VOCABULARY.csv");

            if (File.Exists(gleonFile))
            {
                vocabulary.AddRange(File.ReadAllText(gleonFile).Split(','));
            }
            #endregion

            return vocabulary;
        }

        public static void Add(string text)
        {
            if (!Vocabulary.Contains(text))
                Vocabulary.Add(text);
            SaveVocabulary();
        }
    }
}
