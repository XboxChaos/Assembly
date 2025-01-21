
namespace Assembly.Helpers
{
	public enum TagDataCommandState
	{
		None,
		NotMainArea,
		FirstGenCache,
		EarlyThirdGenCache,
		Eldorado
	}

	public class TagDataCommandStateResolver
	{
		private static string _none = "The selected action is supported. (You should not be seeing this message!)";
		private static string _notMainArea = "Only tags scoped to the cache file's main tag data area are currently supported for the selected action.";
		private static string _firstGen = "First generation cache files are not currently supported for the selected action.";
		private static string _earlyThirdGen = "Early third generation cache files are not currently supported for the selected action.";
		private static string _eldorado = "Halo Online files are not currently supported for the selected action.";
		private static string _unknown = "The selected action is not supported.";

		public static string GetStateDescription(TagDataCommandState state)
		{
			switch (state)
			{
				case TagDataCommandState.None:
					return _none;

				case TagDataCommandState.NotMainArea:
					return _notMainArea;
				case TagDataCommandState.FirstGenCache:
					return _firstGen;
				case TagDataCommandState.EarlyThirdGenCache:
					return _earlyThirdGen;
				case TagDataCommandState.Eldorado:
					return _eldorado;

				default:
					return _unknown;
			}
		}
	}
}
