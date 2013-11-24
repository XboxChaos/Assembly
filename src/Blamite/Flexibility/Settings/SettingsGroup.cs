using System;
using System.Collections.Generic;
using System.Globalization;
using Blamite.Util;

namespace Blamite.Flexibility.Settings
{
	/// <summary>
	///     A group of settings.
	/// </summary>
	public class SettingsGroup
	{
		private readonly Dictionary<string, SettingsGroup> _groups = new Dictionary<string, SettingsGroup>();
		private readonly Dictionary<string, object> _settings = new Dictionary<string, object>();

		/// <summary>
		///     Gets the value of a setting by path.
		/// </summary>
		/// <typeparam name="T">The requested type to return the setting as.</typeparam>
		/// <param name="path">The path to the setting to retrieve.</param>
		/// <returns>The value of the setting.</returns>
		public T GetSetting<T>(string path)
		{
			return GetSettingByPath(path, (group, name) => ConvertSetting<T>(group._settings[name]));
		}

		/// <summary>
		///     Gets the value of a setting by path.
		///     If the setting does not exist, a default value will be returned.
		/// </summary>
		/// <typeparam name="T">The requested type to return the setting as.</typeparam>
		/// <param name="path">The path to the setting to retrieve.</param>
		/// <param name="defaultVal">The value to return if the setting is not found.</param>
		/// <returns>The value of the setting if it exists, or <paramref name="defaultVal" /> otherwise.</returns>
		public T GetSettingOrDefault<T>(string path, T defaultVal)
		{
			if (!PathExists(path))
				return defaultVal;
			return GetSetting<T>(path);
		}

		/// <summary>
		///     Sets the value of a setting by path.
		/// </summary>
		/// <typeparam name="T">The type of the value to set.</typeparam>
		/// <param name="path">The path to the setting to set.</param>
		/// <param name="newValue">The value to assign to the setting.</param>
		public void SetSetting<T>(string path, T newValue)
		{
			SetSettingByPath(path, (group, name) => group._settings[name] = newValue);
		}

		/// <summary>
		///     Gets a sub-group.
		/// </summary>
		/// <param name="path">The path to the group to get.</param>
		/// <returns>The group.</returns>
		public SettingsGroup GetGroup(string path)
		{
			return GetSettingByPath(path, (group, name) => group._groups[name]);
		}

		/// <summary>
		///     Sets a sub-group.
		/// </summary>
		/// <param name="path">The path to the sub-group to set.</param>
		/// <param name="newGroup">The value of the group.</param>
		public void SetGroup(string path, SettingsGroup newGroup)
		{
			SetSettingByPath(path, (group, name) => group._groups[name] = newGroup);
		}

		/// <summary>
		///     Determines whether or not a given path has a value defined for it.
		/// </summary>
		/// <param name="path">The path to check.</param>
		/// <returns><c>true</c> if the path has a value defined.</returns>
		public bool PathExists(string path)
		{
			string[] pathComponents = SplitPath(path);
			SettingsGroup currentGroup = this;
			for (int i = 0; i < pathComponents.Length; i++)
			{
				string component = pathComponents[i];
				if (i == pathComponents.Length - 1)
				{
					return currentGroup._settings.ContainsKey(component)
					       || currentGroup._groups.ContainsKey(component);
				}
				if (!currentGroup._groups.TryGetValue(component, out currentGroup))
					return false;
			}
			return false;
		}

		/// <summary>
		///     Deep-clones the setting group.
		/// </summary>
		/// <returns>The new group.</returns>
		public SettingsGroup DeepClone()
		{
			var result = new SettingsGroup();
			foreach (var setting in _settings)
				result._settings[setting.Key] = setting.Value;
			foreach (var group in _groups)
				result._groups[group.Key] = group.Value.DeepClone();
			return result;
		}

		/// <summary>
		///     Imports settings recursively from another group.
		///     Any existing settings will be overwritten, and all sub-groups will be merged.
		/// </summary>
		/// <param name="other">The group to import from.</param>
		public void Import(SettingsGroup other)
		{
			foreach (var setting in other._settings)
				_settings[setting.Key] = setting.Value;

			foreach (var group in other._groups)
			{
				SettingsGroup existingGroup;
				if (_groups.TryGetValue(group.Key, out existingGroup))
					existingGroup.Import(group.Value);
				else
					_groups[group.Key] = group.Value;
			}
		}

		/// <summary>
		///     Gets the value of a setting by path.
		/// </summary>
		/// <typeparam name="T">The type of the setting.</typeparam>
		/// <param name="path">The path of the value to get.</param>
		/// <param name="getter">A function that gets the value once the bottom-level group is found.</param>
		/// <returns>The value of the setting, as returned by the getter.</returns>
		private T GetSettingByPath<T>(string path, Func<SettingsGroup, string, T> getter)
		{
			string[] pathComponents = SplitPath(path);
			SettingsGroup currentGroup = this;
			for (int i = 0; i < pathComponents.Length; i++)
			{
				string component = pathComponents[i];
				if (i == pathComponents.Length - 1)
					return getter(currentGroup, component);
				currentGroup = currentGroup._groups[component];
			}
			throw new ArgumentException("Invalid path");
		}

		/// <summary>
		///     Sets the value of a setting by path. Any groups will be created along the way.
		/// </summary>
		/// <param name="path">The path to the setting.</param>
		/// <param name="setter">An action that sets the value once the bottom-level group is found.</param>
		private void SetSettingByPath(string path, Action<SettingsGroup, string> setter)
		{
			string[] pathComponents = SplitPath(path);
			SettingsGroup currentGroup = this;
			for (int i = 0; i < pathComponents.Length; i++)
			{
				string component = pathComponents[i];
				if (i == pathComponents.Length - 1)
				{
					setter(currentGroup, component);
					return;
				}
				if (!currentGroup._groups.TryGetValue(component, out currentGroup))
				{
					// Sub-group does not exist - create one
					var newGroup = new SettingsGroup();
					currentGroup._groups[component] = newGroup;
					currentGroup = newGroup;
				}
			}
		}

		/// <summary>
		///     Splits a path into its components.
		/// </summary>
		/// <param name="path">The path to split.</param>
		/// <returns>An array where each element is a component of the path.</returns>
		private string[] SplitPath(string path)
		{
			return path.Split('/');
		}

		/// <summary>
		///     Converts a setting to a requested type.
		/// </summary>
		/// <typeparam name="T">The requested type.</typeparam>
		/// <param name="setting">The setting to convert.</param>
		/// <returns>The converted setting.</returns>
		private T ConvertSetting<T>(object setting)
		{
			Type type = typeof (T);
			if (TypeUtil.IsInteger(type))
			{
				if (TypeUtil.IsInteger(setting.GetType()))
					return (T) Convert.ChangeType(setting, type);
				return (T) Convert.ChangeType(ParseLong(setting.ToString()), type);
			}
			if (type == typeof (string))
			{
				return (T) (object) setting.ToString();
			}
			return (T) Convert.ChangeType(setting, type);
		}

		private long ParseLong(string str)
		{
			if (str.StartsWith("-0x"))
				return -long.Parse(str.Substring(3), NumberStyles.HexNumber);
			if (str.StartsWith("0x"))
				return long.Parse(str.Substring(2), NumberStyles.HexNumber);
			return long.Parse(str);
		}
	}
}