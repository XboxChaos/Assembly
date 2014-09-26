using System;
using System.Collections.ObjectModel;
using System.Globalization;
using Blamite.IO;
using System.Collections.Generic;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	public class ReflexiveData : ValueField
	{
		public ReflexiveData(string name, uint offset, uint address, uint entrySize, uint pluginLine, FileSegmentGroup metaArea)
			: base(name, offset, address, pluginLine)
		{
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitReflexive(this);
		}

		public override MetaField CloneValue()
		{
			// TODO: Fix
			return new ReflexiveData(Name, Offset, FieldAddress, 0, PluginLine, null);
		}
	}
}