using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Blam.Shaders;
using Blamite.Plugins;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	public class ShaderRef : ValueField
	{
		private IShader _shader;
		private string _dbString;

		public ShaderRef(string name, uint offset, long address, ShaderType type, IShader value, uint pluginLine, string tooltip)
			: base(name, offset, address, pluginLine, tooltip)
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
				NotifyPropertyChanged("IsValid");
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
				NotifyPropertyChanged("DatabasePath");
			}
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitShaderRef(this);
		}

		public override MetaField CloneValue()
		{
			return new ShaderRef(Name, Offset, FieldAddress, Type, Shader, PluginLine, ToolTip);
		}

		public override string AsString()
		{
			return string.Format("shader | {0} | {1}", Name, IsValid ? "valid" : "invalid");
		}
	}
}
