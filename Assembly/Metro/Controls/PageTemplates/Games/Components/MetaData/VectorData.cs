using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    public class VectorData : ValueField
    {
        private float _x, _y, _z;

        public VectorData(string name, uint offset, float x, float y, float z, uint pluginLine)
            : base(name, offset, pluginLine)
        {
            _x = x;
            _y = y;
            _z = z;
        }

        public float X
        {
            get { return _x; }
            set { _x = value; NotifyPropertyChanged("X"); }
        }

        public float Y
        {
            get { return _y; }
            set { _y = value; NotifyPropertyChanged("Y"); }
        }

        public float Z
        {
            get { return _z; }
            set { _z = value; NotifyPropertyChanged("Z"); }
        }

        public override void Accept(IMetaFieldVisitor visitor)
        {
            visitor.VisitVector(this);
        }

        public override MetaField CloneValue()
        {
            return new VectorData(Name, Offset, _x, _y, _z, base.PluginLine);
        }
    }
}
