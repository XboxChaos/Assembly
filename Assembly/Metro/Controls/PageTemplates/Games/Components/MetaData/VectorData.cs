using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
    public class VectorData : ValueField
    {
        private float _x, _y, _z;
        private float _originalX, _originalY, _originalZ;

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

        public override MetaField DeepClone()
        {
            VectorData result = new VectorData(Name, Offset, _x, _y, _z, base.PluginLine);
            result._originalX = _originalX;
            result._originalY = _originalY;
            result._originalZ = _originalZ;
            return result;
        }

        public override bool HasChanged
        {
            get { return _x != _originalX || _y != _originalY || _z != _originalZ; }
        }

        public override void Reset()
        {
            X = _originalX;
            Y = _originalY;
            Z = _originalZ;
        }

        public override void KeepChanges()
        {
            _originalX = _x;
            _originalY = _y;
            _originalZ = _z;
        }
    }
}
