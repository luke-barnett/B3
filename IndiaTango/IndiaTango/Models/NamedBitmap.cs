using System;
using System.Drawing;
using System.Runtime.Serialization;

namespace IndiaTango.Models
{
    [Serializable]
    [DataContract]
    public class NamedBitmap : ISerializable
    {
        private Bitmap _bitmap;
        private string _name;

        protected NamedBitmap(SerializationInfo info, StreamingContext context)
        {
            _name = info.GetString("Name");
            _bitmap = info.GetValue("Bitmap", typeof (Bitmap)) as Bitmap;
        }

        private NamedBitmap() { }


        public NamedBitmap(Bitmap bitmap, string name)
        {
            _bitmap = bitmap;
            _name = name;
        }

        [DataMember]
        public Bitmap Bitmap
        {
            get { return _bitmap; }
            set { _bitmap = value; }
        }

        [DataMember]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Name", Name, typeof(string));
            info.AddValue("Bitmap", Bitmap, typeof(Bitmap));
        }
    }
}
