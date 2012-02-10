using System;
using System.Drawing;
using ProtoBuf;

namespace IndiaTango.Models
{
    /// <summary>
    /// A bitmap that has an applied name
    /// </summary>
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

        /// <summary>
        /// The bitmap
        /// </summary>
        [ProtoMember(1)]
        public Bitmap Bitmap
        {
            get { return _bitmap; }
            set { _bitmap = value; }
        }

        /// <summary>
        /// The name of the bitmap
        /// </summary>
        [ProtoMember(2)]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
    }
}
