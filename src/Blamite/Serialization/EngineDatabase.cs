using Blamite.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Blamite.Serialization
{
	/// <summary>
	///     A database of engine descriptions indexable by name and version string.
	/// </summary>
	public class EngineDatabase : IEnumerable<EngineDescription>
	{
		private readonly Dictionary<string, EngineDescription> _enginesByName = new Dictionary<string, EngineDescription>();
		private readonly List<EngineDescription> _engines = new List<EngineDescription>();

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
			_engines.Add(engine);
		}

		/// <summary>
		///     Finds engine descriptions that use a specific version number.
		/// </summary>
		/// <param name="version">The version number of the EngineDescriptions to find.</param>
		/// <param name="endian">The endian of the EngineDescriptions to find.</param>
		/// <returns>A list containing matching EngineDescriptions</returns>
		public List<EngineDescription> FindEnginesByVersion(int version, Endian endian)
		{
			List<EngineDescription> results = _engines.Where(x => x.Version == version || x.VersionAlt == version)
				.Where(x => x.Endian == endian).ToList();
			return results;
		}

		/// <summary>
		///     Finds an engine description by its build string. 
		/// </summary>
		/// <param name="build">The build string of the engine description to find.</param>
		/// <returns>The description if found, or <c>null</c> otherwise.</returns>
		public EngineDescription FindEngineByBuild(string build)
		{
			EngineDescription result = _engines.Where(x => x.BuildVersion == build).FirstOrDefault();
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