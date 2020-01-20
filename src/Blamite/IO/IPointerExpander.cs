using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.IO
{
	/// <summary>
	///     Interface for a class which expands and contracts pointers.
	/// </summary>
	public interface IPointerExpander
	{
		bool IsValid { get; }

		long Expand(uint pointer);

		uint Contract(long pointer);
	}
}
