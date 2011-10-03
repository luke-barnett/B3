using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace IndiaTango.Models
{
    [Serializable]
    [DataContract]
    public class NamedBitmap
    {
        private Bitmap _bitmap;
        private string _name;

        private NamedBitmap(){ }


        public NamedBitmap(Bitmap bitmap, string name)
        {
            _bitmap = bitmap;
            _name = name;
        }

        [DataMember]
        public Bitmap Bitmap
        {
            get { return _bitmap; }
            set { _bitmap = value;  }
        }

        [DataMember]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
    }
}
