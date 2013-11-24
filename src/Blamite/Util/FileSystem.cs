namespace Blamite.Util
{
	public class FileSystem
	{
		public static char[] DissallowedNTFSChars = {'\\', '/', ':', '?', '"', '<', '>', '|'};

		/// <summary>
		///     Strips disallowed chars from the input string
		/// </summary>
		/// <param name="input"></param>
		/// <returns>Stripped string, that doesn't contain any 'UnallowedNTFSChars'</returns>
		public static string StripNoNTFSAllowedNames(string input)
		{
			string output = input;
			foreach (char unallowedChar in DissallowedNTFSChars)
				output = output.Replace(unallowedChar, ' ');

			return output;
		}
	}
}