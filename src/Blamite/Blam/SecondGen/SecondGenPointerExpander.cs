using Blamite.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.SecondGen
{
	public class SecondGenPointerExpander : IPointerExpander
	{
		public long Expand(uint pointer)
		{
			return pointer;
		}

		public uint Contract(long pointer)
		{
			return (uint)pointer;
		}
	}
}
