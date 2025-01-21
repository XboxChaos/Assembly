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
		protected readonly EngineDescription _buildInfo;

		protected MCCRTEProvider(EngineDescription engine)
		{
			_buildInfo = engine;
		}

		/// <summary>
		///     The type of connection that the provider will establish.
		///     Always RTEConnectionType.LocalProcess.
		/// </summary>
		public RTEConnectionType ConnectionType
		{
			get { return RTEConnectionType.LocalProcess64; }
		}

		public abstract IStream GetMetaStream(ICacheFile cacheFile = null, ITag tag = null);

		protected bool CheckBuildInfo()
		{
			if (string.IsNullOrEmpty(_buildInfo.PokingExecutable))
				throw new InvalidOperationException("No gameExecutable value found in Engines.xml for engine " + _buildInfo.Name + ".");
			if (string.IsNullOrEmpty(_buildInfo.PokingModule))
				throw new InvalidOperationException("No gameModule value found in Engines.xml for engine " + _buildInfo.Name + ".");
			if (_buildInfo.Poking == null)
				throw new InvalidOperationException("No poking definitions found in Engines.xml for engine " + _buildInfo.Name + ".");

			return true;
		}

		protected PokingInformation RetrieveInformation(Process gameProcess)
		{
#if X86
			throw new InvalidOperationException("Cannot access a 64bit process with a 32bit program.");
#else
			//verify version, and check for anticheat at the same time
			string version = "";
			PokingInformation info = null;
			try
			{
				version = gameProcess.MainModule.FileVersionInfo.FileVersion;

				//TODO: make winstore support not horrible
				if (version == null)
					info = _buildInfo.Poking.RetrieveLatestInfo();
				else
					info = _buildInfo.Poking.RetrieveInformation(version);

				if (info == null)
					throw new InvalidOperationException("Game version " + version + " does not have poking information defined in the Formats folder.");
			}
			catch (System.ComponentModel.Win32Exception)
			{
				throw new InvalidOperationException("Cannot access game process. This could be due to Anti-Cheat or lack of admin privileges.");
			}

			return info;
#endif
		}

		protected Process FindGameProcess()
		{
			Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(_buildInfo.PokingExecutable));

			if (processes.Length > 0)
				return processes[0];

			if (!string.IsNullOrEmpty(_buildInfo.PokingExecutableAlt))
			{
				processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(_buildInfo.PokingExecutableAlt));
				return processes.Length > 0 ? processes[0] : null;
			}
			else
				return null;
		}

	}
}
