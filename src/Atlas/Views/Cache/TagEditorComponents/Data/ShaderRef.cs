using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Atlas.Views.Cache.TagEditorComponents.Data;
using Blamite.Blam.Shaders;
using Blamite.Plugins;

namespace Atlas.Views.Cache.TagEditorComponents.Data
{
	public class ShaderRef : ValueField
	{
		private IShader _shader;
		private string _dbString;

		public ShaderRef(string name, uint offset, uint address, ShaderType type, IShader value, uint pluginLine)
			: base(name, offset, address, pluginLine)
		{
			Type = type;
			Shader = value;
		}

		public IShader Shader
		{
			get { return _shader; }
			set
			{
				_shader = value;
				if (_shader != null && _shader.DatabasePath != null)
					DatabasePath = _shader.DatabasePath.Substring(_shader.DatabasePath.LastIndexOf('\\') + 1);
				else
					DatabasePath = "";
				OnPropertyChanged("IsValid");
			}
		}

		public bool IsValid
		{
			get { return Shader != null; }
		}

		public ShaderType Type { get; private set; }

		public string DatabasePath
		{
			get { return _dbString; }
			private set
			{
				_dbString = value;
				OnPropertyChanged("DatabasePath");
			}
		}

		public override void Accept(ITagDataFieldVisitor visitor)
		{
			visitor.VisitShaderRef(this);
		}

		public override TagDataField CloneValue()
		{
			return new ShaderRef(Name, Offset, FieldAddress, Type, Shader, PluginLine);
		}
	}
}
