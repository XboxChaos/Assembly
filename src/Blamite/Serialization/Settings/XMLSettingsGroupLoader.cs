using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Blamite.Util;

namespace Blamite.Serialization.Settings
{
	/// <summary>
	///     Loads settings groups from XML data.
	/// </summary>
	public class XMLSettingsGroupLoader
	{
		private readonly Dictionary<string, IComplexSettingLoader> _settingLoaders =
			new Dictionary<string, IComplexSettingLoader>();

		private readonly Dictionary<SettingInfo, object> _settingCache = new Dictionary<SettingInfo, object>();

		/// <summary>
		///     Associates an <see cref="IComplexSettingLoader" /> with a particular type string.
		/// </summary>
		/// <param name="type">The type string to associate the loader with.</param>
		/// <param name="loader">The complex setting loader.</param>
		public void RegisterComplexSettingLoader(string type, IComplexSettingLoader loader)
		{
			_settingLoaders[type] = loader;
		}

		/// <summary>
		///     Loads a settings group from an XML container.
		/// </summary>
		/// <param name="container">The container to read settings from.</param>
		/// <returns>The loaded settings group.</returns>
		public SettingsGroup LoadSettingsGroup(XContainer container)
		{
			var result = new SettingsGroup();
			foreach (XElement elem in container.Elements())
			{
				if (elem.HasElements)
				{
					result.SetGroup(elem.Name.LocalName, LoadSettingsGroup(elem));
				}
				else
				{
					result.SetSetting(elem.Name.LocalName, LoadSetting(elem));
				}
			}
			return result;
		}

		private object LoadSetting(XElement elem)
		{
			// If the elem points to an external setting, then process it
			string type = XMLUtil.GetStringAttribute(elem, "type", null);
			if (type != null)
			{
				string path = XMLUtil.GetStringAttribute(elem, "path", null);
				if (path != null)
				{
					var info = new SettingInfo(type, path);
					if (_settingCache.TryGetValue(info, out object cached))
						return cached;

					IComplexSettingLoader loader;
					if (!_settingLoaders.TryGetValue(type, out loader))
						throw new InvalidOperationException("Unrecognized complex setting type \"" + type + "\"");

					object setting = loader.LoadSetting(path);
					_settingCache[info] = setting;
					return setting;
				}
			}
			return elem.Value;
		}

		private class SettingInfo
		{
			public string Type { get; set; }
			public string Path { get; set; }

			public SettingInfo(string type, string path)
			{
				Type = type;
				Path = path;
			}

			public override bool Equals(object obj)
			{
				if (obj.GetType() != typeof(SettingInfo))
					return false;

				return obj.GetHashCode() == GetHashCode();
			}

			public override int GetHashCode()
			{
				int result = 7057;
				result = result * 8171 + Type.GetHashCode();
				result = result * 8171 + Path.GetHashCode();
				return result;
			}
		}
	}
}