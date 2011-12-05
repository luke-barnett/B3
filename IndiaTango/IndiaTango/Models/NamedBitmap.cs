using System;
using System.Drawing;
using ProtoBuf;

namespace IndiaTango.Models
{
    [Serializable]
    [ProtoContract]
    public class NamedBitmap
    {
        private Bitmap _bitmap;
        private string _name;

        private NamedBitmap() { }

        public NamedBitmap(Bitmap bitmap, string name)
        {
            _bitmap = bitmap;
            _name = name;
        }

        [ProtoMember(1)]
        public Bitmap Bitmap
        {
            get { return _bitmap; }
            set { _bitmap = value; }
        }

        [ProtoMember(2)]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
    }
}
