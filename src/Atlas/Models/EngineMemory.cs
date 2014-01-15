using System;
using System.Collections.ObjectModel;

namespace Atlas.Models
{
	public class EngineMemory
	{
		public ObservableCollection<EngineVersion> EngineVersions { get; set; }

		public class EngineVersion
		{
			public double Version { get; set; }

			public string VersionFriendly { get; set; }

			public ObservableCollection<QuickOption> QuickOptions { get; set; }

			public ObservableCollection<MemoryValue> MemoryValues { get; set; } 

			public class QuickOption
			{
				public string Name { get; set; }

				public string Description { get; set; }

				public UInt32 Address { get; set; }

				public string IconResourceKey { get; set; }

				public bool IsToggle { get; set; }

				public bool CarefulMode { get; set; }
			}

			public class MemoryValue
			{
				public string Name { get; set; }

				public string Description { get; set; }

				public UInt32 Address { get; set; }

				public string Type { get; set; }

				public string Data { get; set; }
			}
		}
	}
}
