using System.Collections;
using System.Collections.Generic;

namespace Blamite.Flexibility
{
	/// <summary>
	///     A database of engine descriptions indexable by name and version string.
	/// </summary>
	public class EngineDatabase : IEnumerable<EngineDescription>
	{
		private readonly Dictionary<string, EngineDescription> _enginesByName = new Dictionary<string, EngineDescription>();
		private readonly Dictionary<string, EngineDescription> _enginesByVersion = new Dictionary<string, EngineDescription>();

		/// <summary>
		///     Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
		/// </returns>
		public IEnumerator<EngineDescription> GetEnumerator()
		{
			return _enginesByName.Values.GetEnumerator();
		}

		/// <summary>
		///     Returns an enumerator that iterates through a collection.
		/// </summary>
		/// <returns>
		///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
		/// </returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return _enginesByName.Values.GetEnumerator();
		}

		/// <summary>
		///     Registers an engine description with the database.
		/// </summary>
		/// <param name="engine">The description to register.</param>
		public void RegisterEngine(EngineDescription engine)
		{
			_enginesByName[engine.Name] = engine;
			_enginesByVersion[engine.Version] = engine;
		}

		/// <summary>
		///     Finds an engine description by its version string.
		/// </summary>
		/// <param name="version">The version string of the engine description to find.</param>
		/// <returns>The description if found, or <c>null</c> otherwise.</returns>
		public EngineDescription FindEngineByVersion(string version)
		{
			EngineDescription result;
			_enginesByVersion.TryGetValue(version, out result);
			return result;
		}

		/// <summary>
		///     Finds an engine description by its name.
		/// </summary>
		/// <param name="name">The name of the engine description to find.</param>
		/// <returns>The description if found, or <c>null</c> otherwise.</returns>
		public EngineDescription FindEngineByName(string name)
		{
			EngineDescription result;
			_enginesByName.TryGetValue(name, out result);
			return result;
		}
	}
}