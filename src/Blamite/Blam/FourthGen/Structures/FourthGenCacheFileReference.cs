using Blamite.Serialization;

namespace Blamite.Blam.FourthGen.Structures
{
	public class FourthGenCacheFileReference
	{
		public FourthGenCacheFileReference(StructureValueCollection values)
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