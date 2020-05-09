using Blamite.Blam;
using Blamite.IO;
using Blamite.Serialization;
using System;
using System.Diagnostics;
using System.IO;

namespace Blamite.RTE
{
	public abstract class MCCRTEProvider : IRTEProvider
	{
		internal readonly EngineDescription _buildInfo;

		internal MCCRTEProvider(EngineDescription engine)
		{
			_buildInfo = engine;
		}

		/// <summary>
		///     The type of connection that the provider will establish.
		///     Always RTEConnectionType.LocalProcess.
		/// </summary>
		public RTEConnectionType ConnectionType
		{
			get { return RTEConnectionType.LocalProcess; }
		}

		public abstract IStream GetMetaStream(ICacheFile cacheFile = null);

		internal bool CheckBuildInfo()
		{
			if (string.IsNullOrEmpty(_buildInfo.GameExecutable))
				throw new InvalidOperationException("No gameExecutable value found in Engines.xml for engine " + _buildInfo.Name + ".");
			if (string.IsNullOrEmpty(_buildInfo.GameModule))
				throw new InvalidOperationException("No gameModule value found in Engines.xml for engine " + _buildInfo.Name + ".");
			if (_buildInfo.Poking == null)
				throw new InvalidOperationException("No poking definitions found in Engines.xml for engine " + _buildInfo.Name + ".");

			return true;
		}

		internal long RetrievePointer(Process gameProcess)
		{
#if X86
			throw new InvalidOperationException("Cannot access a 64bit process with a 32bit program.");
#else
			//verify version, and check for anticheat at the same time
			string version = "";
			long pointer = -1;
			try
			{
				version = gameProcess.MainModule.FileVersionInfo.FileVersion;

				//TODO: make winstore support not horrible
				if (version == null)
					pointer = _buildInfo.Poking.RetrieveLastPointer();
				else
					pointer = _buildInfo.Poking.RetrievePointer(version);

				if (pointer == 0)
					throw new InvalidOperationException("Game version " + version + " does not have a pointer defined in the Formats folder.");
			}
			catch (System.ComponentModel.Win32Exception)
			{
				throw new InvalidOperationException("Cannot access game process. This could be due to Anti-Cheat or lack of admin privileges.");
			}

			return pointer;
#endif
		}


		internal Process FindGameProcess()
		{
			Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(_buildInfo.GameExecutable));

			if (processes.Length > 0)
				return processes[0];

			if (!string.IsNullOrEmpty(_buildInfo.GameExecutableAlt))
			{
				processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(_buildInfo.GameExecutableAlt));
				return processes.Length > 0 ? processes[0] : null;
			}
			else
				return null;
		}

		


	}
}
