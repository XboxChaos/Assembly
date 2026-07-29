using System;

namespace Blamite.Blam.FifthGen
{
	/// <summary>
	///     The exception that is thrown when a fifth-generation tag payload does not match the format the parser expects.
	/// </summary>
	/// <remarks>
	///     This is reserved for conditions which mean the parse has genuinely lost track of the file - a bad signature, a
	///     record count which disagrees with the layout's manifest, or a nested chunk which was not consumed exactly.
	///     Conditions which are merely unusual - a suspect tag reference, an unrecognized field type, an optional chunk nobody
	///     has decoded - are reported through <see cref="FifthGenTagFile.Warnings" /> instead.
	/// </remarks>
	[Serializable]
	public class FifthGenFormatException : Exception
	{
		public FifthGenFormatException()
		{
		}

		public FifthGenFormatException(string message)
			: base(message)
		{
		}

		public FifthGenFormatException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
