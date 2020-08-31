using Blamite.Blam.Scripting.Compiler;
using Blamite.Blam.Scripting.Context;
using Blamite.IO;
using Blamite.Util;
using System;
using System.Collections.Generic;

namespace Blamite.Blam.Scripting
{
	/// <summary>
	///     A script file.
	/// </summary>
	public interface IScriptFile
	{
		/// <summary>
		///     Gets the name of the script file.
		/// </summary>
		/// <value>The name of the script file.</value>
		string Name { get; }

		/// <summary>
		///     Gets the raw script text for the script file. Can be null if not available.
		/// </summary>
		/// <value>The raw script text for the script file, or null if not available.</value>
		string Text { get; }

		/// <summary>
		///     Loads the scripts in the file.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <returns>The scripts that were loaded.</returns>
		ScriptTable LoadScripts(IReader reader);

		/// <summary>
		///     Saves scripts back to the file and reports the progress.
		/// </summary>
		/// <param name="scripts">The scripts to save.</param>
		/// <param name="stream">The stream to operate on.</param>
		/// <param name="progress">The object to report progress with.</param>
		void SaveScripts(ScriptData scripts, IStream stream, IProgress<int> progress);

		/// <summary>
		///		Loads the context for the script file.
		/// </summary>
		/// <param name="reader">The steam to read from.</param>
		/// <param name="cache">The cache file containing the script file.</param>
		/// <returns>The context that was loaded.</returns>
		ScriptingContextCollection LoadContext(IReader reader, ICacheFile cache);

		public IEnumerable<UnitSeatMapping> GetUniqueSeatMappings(IReader reader, ushort opcode);
	}
}