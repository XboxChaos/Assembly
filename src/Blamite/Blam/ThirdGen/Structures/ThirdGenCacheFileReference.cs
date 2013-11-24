using Blamite.Flexibility;

namespace Blamite.Blam.ThirdGen.Structures
{
	public class ThirdGenCacheFileReference
	{
		public ThirdGenCacheFileReference(StructureValueCollection values)
		{
			Load(values);
		}

		public string Path { get; set; }

		private void Load(StructureValueCollection values)
		{
			Path = values.GetString("map path");
		}
	}
}