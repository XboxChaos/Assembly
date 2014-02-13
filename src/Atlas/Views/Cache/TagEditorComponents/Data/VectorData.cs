using Atlas.Views.Cache.TagEditorComponents.Data;

namespace Atlas.Views.Cache.TagEditorComponents.Data
{
	public class VectorData : ValueField
	{
		private float _x, _y, _z;

		public VectorData(string name, uint offset, uint address, float x, float y, float z, uint pluginLine)
			: base(name, offset, address, pluginLine)
		{
			_x = x;
			_y = y;
			_z = z;
		}

		public float X
		{
			get { return _x; }
			set
			{
				_x = value;
				OnPropertyChanged("X");
			}
		}

		public float Y
		{
			get { return _y; }
			set
			{
				_y = value;
				OnPropertyChanged("Y");
			}
		}

		public float Z
		{
			get { return _z; }
			set
			{
				_z = value;
				OnPropertyChanged("Z");
			}
		}

		public override void Accept(ITagDataFieldVisitor visitor)
		{
			visitor.VisitVector(this);
		}

		public override TagDataField CloneValue()
		{
			return new VectorData(Name, Offset, FieldAddress, _x, _y, _z, base.PluginLine);
		}
	}
}