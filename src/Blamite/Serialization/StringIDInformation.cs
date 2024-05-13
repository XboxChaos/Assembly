using Blamite.Blam;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Serialization
{
	public class StringIDInformation
	{
		private readonly List<StringIDNamespace> _namespaces;

		public StringIDInformation(StringIDLayout idLayout)
		{
			IDLayout = idLayout;
			_namespaces = new List<StringIDNamespace>();
		}

		/// <summary>
		///     Gets the layout of stringIDs.
		/// </summary>
		public StringIDLayout IDLayout { get; private set; }

		public List<StringIDNamespace> Namespaces
		{
			get
			{
				return _namespaces;
			}
		}

		/// <summary>
		///     Registers a stringID set to use to translate stringIDs.
		/// </summary>
		/// <param name="id">The set's ID number.</param>
		/// <param name="minIndex">The minimum index that a stringID must have in order to be counted as part of the set.</param>
		/// <param name="globalIndex">The index of the set's first string in the global stringID table.</param>
		public void AddNamespace(int id, int minIndex, int globalIndex)
		{
			_namespaces.Add(new StringIDNamespace(id, minIndex, globalIndex));
		}
	}
}
