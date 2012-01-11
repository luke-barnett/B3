using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using ProtoBuf;

namespace IndiaTango.Models
{
    /// <summary>
    /// Simple reason object that has the change number it is associated with and it's reason
    /// </summary>
    [DataContract]
    [ProtoContract]
    public class ChangeReason
    {
        public ChangeReason() {} //FOR PROTOBUF

        public ChangeReason(int id, string reason)
        {
            ID = id;
            Reason = reason;
        }

        /// <summary>
        /// The ID of the change
        /// </summary>
        [DataMember]
        [ProtoMember(1)]
        public int ID { get; private set; }

        /// <summary>
        /// The reason for the change
        /// </summary>
        [DataMember]
        [ProtoMember(2)]
        public string Reason { get; private set; }

        public override string ToString()
        {
            return string.Format("[{0}] {1}", ID, Reason);
        }

        public static string FileLocation = Path.Combine(Common.AppDataPath, "ChangeReasons.xml");

        public static string DefaultReasonsFileLocation = Path.Combine(Assembly.GetExecutingAssembly().Location.Replace("B3.exe", ""), "Resources", "defaultreasons.xml");

        private static List<ChangeReason> GetChangeReasons()
        {
            if (!File.Exists(FileLocation))
                return GenerateDefaultReasons();


            using (var fileStream = File.OpenRead(FileLocation))
                return
                    new DataContractSerializer(typeof(List<ChangeReason>)).ReadObject(fileStream) as
                    List<ChangeReason>;

        }

        private static List<ChangeReason> _changeReasons;
        public static List<ChangeReason> ChangeReasons
        {
            get { return _changeReasons ?? (_changeReasons = GetChangeReasons()); }
        }

        private static void SaveChangeReasons()
        {
            if (_changeReasons == null)
                return;

            using (var fileStream = File.Create(FileLocation))
                new DataContractSerializer(typeof(List<ChangeReason>)).WriteObject(fileStream, ChangeReasons);
        }

        public static ChangeReason AddNewChangeReason(string reason)
        {
            var changeReason = new ChangeReason(ChangeReasons.Count + 1, reason);
            if (reason != null)
            {
                ChangeReasons.Add(changeReason);
                SaveChangeReasons();
            }
            else
                return new ChangeReason(-1, "Reason not specified");

            return changeReason;
        }

        private static List<ChangeReason> GenerateDefaultReasons()
        {
            if (!File.Exists(DefaultReasonsFileLocation))
                return new List<ChangeReason>();

            using (var fileStream = File.OpenRead(DefaultReasonsFileLocation))
                return
                    new DataContractSerializer(typeof(List<ChangeReason>)).ReadObject(fileStream) as
                    List<ChangeReason>;
        }
    }
}
