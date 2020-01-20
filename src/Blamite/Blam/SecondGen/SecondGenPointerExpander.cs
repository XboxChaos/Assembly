using Blamite.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.SecondGen
{
	public class SecondGenPointerExpander : IPointerExpander
	{
		public bool IsValid { get { return false; } }//lol

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
