using Blamite.Blam;
using Blamite.IO;

namespace Blamite.RTE.Console
{
	public class ConsoleRTEProvider : RTEProvider
	{
		private readonly XConsole _console;

		/// <summary>
		///     Constructs a new ConsoleRTEProvider based off of an BaseConsole object.
		/// </summary>
		/// <param name="console">The BaseConsole object to use to interact with the console.</param>
		public ConsoleRTEProvider(XConsole console) : base(null)
		{
			_console = console;
		}

		/// <summary>
		///     The type of connection that the provider will establish.
		/// </summary>
		public override RTEConnectionType ConnectionType
		{
			get { return _console.ConnectionType; }
		}

		/// <summary>
		///     Obtains a stream which can be used to read and write a cache file's meta in realtime.
		///     The stream will be set up such that offsets in the stream correspond to meta pointers in the cache file.
		/// </summary>
		/// <param name="cacheFile">The cache file to get a stream for.</param>
		/// <param name="tag">The tag to be poked; only needed for Eldorado.</param>
		/// <returns>The stream if it was opened successfully, or null otherwise.</returns>
		public override IStream GetCacheStream(ICacheFile cacheFile, ITag tag = null)
		{
			// Okay, so technically we should be checking to see if the cache file is actually loaded into memory first
			// But that's kinda hard to do...
			if (!_console.Connect())
			{
				ErrorMessage = "Could not connect to the console " + _console.Identifier + ". Double check the console is powered on and connected to the same local network.";
				return null;
			}

			_console.Disconnect();
			return new EndianStream(_console.ConsoleStream, _console.Endianness);
		}
	}
}
