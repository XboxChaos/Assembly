using Blamite.Blam;
using Blamite.IO;
using Blamite.Serialization;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace Blamite.RTE
{
	/// <summary>
	///     Real-time editing connection types.
	/// </summary>
	public enum RTEConnectionType
	{
		None,
		ConsoleXbox,
		ConsoleXbox360,
		LocalProcess32,
		LocalProcess64
	}

	public abstract class BaseRTEProvider
	{
		protected readonly EngineDescription _buildInfo;
		protected bool _hadToGuessVersion = false;

		protected BaseRTEProvider(EngineDescription engine)
		{
			_buildInfo = engine;
		}

		public RTEConnectionType ConnectionType
		{
			get { return RTEConnectionType.LocalProcess32; }
		}

		protected string GuessError
		{
			get
			{
				return _hadToGuessVersion ?
				"\r\n\r\nNOTE: The game version could not be confirmed and fell back to the latest entry so this error could be the result of outdated poking definitions." :
				string.Empty;
			}
		}

		/// <summary>
		/// If GetCacheStream returns null, this should explain why.
		/// </summary>
		public string ErrorMessage { get; protected set; }

		/// <summary>
		///     Obtains a stream which can be used to read and write a cache file's meta in realtime.
		///     The stream will be set up such that offsets in the stream correspond to meta pointers in the cache file.
		/// </summary>
		/// <param name="cacheFile">The cache file to get a stream for.</param>
		/// <returns>The stream if it was opened successfully, or null otherwise, with <seealso cref="ErrorMessage"/> containing the reason.</returns>
		public abstract IStream GetCacheStream(ICacheFile cacheFile);

		/// <summary>
		///		Verifies if necessary information is present in Engines.xml for the engine to allow for poking.
		/// </summary>
		/// <returns>Whether required data is present. If not, <seealso cref="ErrorMessage"/> will contain the reason.</returns>
		protected bool CheckBuildInfo()
		{
			if (string.IsNullOrEmpty(_buildInfo.PokingExecutable))
			{
				ErrorMessage = "No gameExecutable value found in Engines.xml for engine " + _buildInfo.Name + ".";
				return false;
			}
			if (_buildInfo.Poking == null)
			{
				ErrorMessage = "No poking definitions found in Engines.xml for engine " + _buildInfo.Name + ".";
				return false;
			}

			return true;
		}

		/// <summary>
		///		Gets the version of the game process and looks for a poking definition for it.
		/// </summary>
		/// <param name="gameProcess">The game process to check.</param>
		/// <param name="gameModule">Optional. The game module (from the given process) to check. Can be null.</param>
		/// <returns>The poking definition for the version if found, null if not, with <seealso cref="ErrorMessage"/> containing the reason.</returns>
		protected PokingInformation RetrieveInformation(Process gameProcess, ProcessModule gameModule = null)
		{
			//verify version, and check for anticheat at the same time
			string version = "";
			_hadToGuessVersion = false;
			PokingInformation info = null;
			try
			{
				//TODO: make winstore support not horrible; have user set their modifiablewindowsapps dir so fileversioninfo can actually be read? trying to read it from the process memory sounds like a pain in the ass
				//then again touching \Program Files will probably want admin or something and thats gross.

				//check against module version if applicable in case there is a mismatch by the user for some reason
				if (gameModule != null)
					version = gameModule.FileVersionInfo.FileVersion;
				else
					version = gameProcess.MainModule.FileVersionInfo.FileVersion;

				info = _buildInfo.Poking.RetrieveInformation(version);

				if (info == null)
				{
					//version couldn't be found for whatever reason, so fall back to latest definition and prepare to warn the user that we did so if any errors occur.
					info = _buildInfo.Poking.RetrieveLatestInfo();

					if (info == null)
					{
						ErrorMessage = "Game version " + version + " does not have poking information defined in the Formats folder and no other poking information was available to fall back on.";
						return null;
					}
					_hadToGuessVersion = true;
				}
			}
			catch (System.ComponentModel.Win32Exception e)
			{
				ErrorMessage = "Cannot access game process. The following exception occured:\r\n\"" + e.Message + "\"\r\n\r\nThis could be due to Anti-Cheat or lack of admin privileges.";
				return null;
			}

			return info;
		}

		/// <summary>
		///		Tries to find a running game process using <seealso cref="EngineDescription.PokingExecutable"/> and/or <seealso cref="EngineDescription.PokingExecutableAlt"/>.
		/// </summary>
		/// <returns>The first instance of the found process, otherwise null, with <seealso cref="ErrorMessage"/> containing the reason.</returns>
		protected Process FindGameProcess()
		{
			Process result = FindGameProcessByName(_buildInfo.PokingExecutable);
			if (result != null)
				return result;

			if (!string.IsNullOrEmpty(_buildInfo.PokingExecutableAlt))
			{
				result = FindGameProcessByName(_buildInfo.PokingExecutableAlt);
				if (result != null)
					return result;
				else
				{
					ErrorMessage = "Game process \"" + _buildInfo.PokingExecutable + "\" (or \"" + _buildInfo.PokingExecutableAlt + "\") does not appear to be running.";
					return null;
				}
			}
			else
			{
				ErrorMessage = "Game process \"" + _buildInfo.PokingExecutable + "\" does not appear to be running.";
				return null;
			}
		}

		private Process FindGameProcessByName(string name)
		{
			Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(name));

			if (processes.Length > 0)
				return processes[0];

			return null;
		}

		/// <summary>
		///		Tries to find a running game module using <seealso cref="EngineDescription.PokingModule"/>.
		/// </summary>
		/// <param name="process">The process to search.</param>
		/// <param name="errorOccured">Indicates that <seealso cref="EngineDescription.PokingModule"/> was defined but was not found in the process, and that <seealso cref="ErrorMessage"/> was written to.</param>
		/// <returns>The first instance of the found module if applicable. Returns null if <seealso cref="EngineDescription.PokingModule"/> is undefined, or if it was defined but not found, in which case <paramref name="errorOccured"/> will be true and <seealso cref="ErrorMessage"/> contains the reason.</returns>
		protected ProcessModule FindGameModule(Process process, out bool errorOccured)
		{
			errorOccured = false;
			if (!string.IsNullOrEmpty(_buildInfo.PokingModule))
			{
				try
				{
					foreach (ProcessModule m in process.Modules)
						if (Path.GetFileNameWithoutExtension(m.FileName) == _buildInfo.PokingModule)
							return m;

					ErrorMessage = "Game process \"" + _buildInfo.PokingExecutable + "\" does not appear to be running any module named \"" + _buildInfo.PokingModule + "\".";
					errorOccured = true;
				}
				catch (System.ComponentModel.Win32Exception e)
				{
					ErrorMessage = "Cannot access game process. The following exception occured:\r\n\"" + e.Message + "\"\r\n\r\nThis could be due to Anti-Cheat or lack of admin privileges.";
					errorOccured = true;

					return null;
				}
			}

			return null;
		}
	}
}
