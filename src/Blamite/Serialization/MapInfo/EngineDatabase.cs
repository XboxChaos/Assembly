using System;
using System.Collections;
using System.Collections.Generic;

namespace Blamite.Serialization.MapInfo
{
	/// <summary>
	///     A database of engine descriptions indexable by name and version string.
	/// </summary>
	public class EngineDatabase : IEnumerable<EngineDescription>
	{
		private readonly Dictionary<long, EngineDescription> _engines = new Dictionary<long, EngineDescription>();

		/// <summary>
		///     Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
		/// </returns>
		public IEnumerator<EngineDescription> GetEnumerator()
		{
			return _engines.Values.GetEnumerator();
		}

		/// <summary>
		///     Returns an enumerator that iterates through a collection.
		/// </summary>
		/// <returns>
		///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
		/// </returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return _engines.Values.GetEnumerator();
		}

		/// <summary>
		///     Registers an engine description with the database.
		/// </summary>
		/// <param name="engine">The description to register.</param>
		public void RegisterEngine(EngineDescription engine)
		{
			_engines[((long)engine.LevlSize << 32) | engine.Version] = engine;
		}

		/// <summary>
		///     Finds an engine description by its version.
		/// </summary>
		/// <param name="version">The version of the engine description to find.</param>
		/// <param name="size">The levl chunk size of the engine description to find.</param>
		/// <returns>The description if found, or <c>null</c> otherwise.</returns>
		public EngineDescription FindEngine(int size, int version)
		{
			EngineDescription result;
			_engines.TryGetValue(((long)size << 32) | version, out result);
			return result;
		}
	}
}