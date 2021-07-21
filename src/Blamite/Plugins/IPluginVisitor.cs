using Blamite.Blam.Shaders;
namespace Blamite.Plugins
{
	/// <summary>
	///     An interface for a class which handles data read from plugins.
	/// </summary>
	public interface IPluginVisitor
	{
		/// <summary>
		///     Called when a plugin is started to be read.
		/// </summary>
		/// <param name="baseSize">The size of the group's base metadata, or 0 if not known.</param>
		/// <returns>False if the contents of the plugin should not be read.</returns>
		bool EnterPlugin(int baseSize);

		/// <summary>
		///     Called after the contents of a plugin have been read.
		/// </summary>
		void LeavePlugin();

		/// <summary>
		///     Called before a group of revisions begins.
		/// </summary>
		/// <returns>False if the revisions should be skipped.</returns>
		bool EnterRevisions();

		/// <summary>
		///     Called when a revision tag is encountered in the plugin.
		/// </summary>
		/// <param name="revision">Information about the revision.</param>
		void VisitRevision(PluginRevision revision);

		/// <summary>
		///     Called after a group of revisions has been read.
		/// </summary>
		void LeaveRevisions();

		/// <summary>
		///     Called when a comment tag is encountered in the plugin.
		/// </summary>
		/// <param name="title">The comment's title.</param>
		/// <param name="text">The comment's text.</param>
		/// <param name="pluginLine">The line the tag originated from.</param>
		void VisitComment(string title, string text, uint pluginLine);

		// These are called whenever basic values are found in the plugin.
		// Parameters should be fairly self-explanatory.
		void VisitUInt8(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		void VisitInt8(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		void VisitUInt16(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		void VisitInt16(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		void VisitUInt32(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		void VisitInt32(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		void VisitUInt64(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		void VisitInt64(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		void VisitFloat32(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		void VisitUndefined(string name, uint offset, bool visible, uint pluginLine, string tooltip);

		void VisitDatum(string name, uint offset, bool visible, uint pluginline, string tooltip);

		void VisitPoint2(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		void VisitPoint3(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		void VisitVector2(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		void VisitVector3(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		void VisitVector4(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		void VisitDegree(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		void VisitDegree2(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		void VisitDegree3(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		void VisitPlane2(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		void VisitPlane3(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		void VisitRect16(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		void VisitQuat16(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		void VisitPoint16(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		void VisitStringID(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		void VisitTagReference(string name, uint offset, bool visible, bool withGroup, uint pluginLine, string tooltip);
		void VisitDataReference(string name, uint offset, string format, bool visible, int align, uint pluginLine, string tooltip);

		void VisitRangeInt16(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		void VisitRangeFloat32(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		void VisitRangeDegree(string name, uint offset, bool visible, uint pluginLine, string tooltip);

		/// <summary>
		///     Called when a raw data block is encountered in the plugin.
		/// </summary>
		/// <param name="name">The block's name.</param>
		/// <param name="offset">The block's offset.</param>
		/// <param name="visible">True if the block is visible.</param>
		/// <param name="size">The size of the block.</param>
		/// <param name="pluginLine">The line in the plugin this entry is found.</param>
		void VisitRawData(string name, uint offset, bool visible, int size, uint pluginLine, string tooltip);

		/// <summary>
		///     Called when an ASCII string is encountered in the plugin.
		/// </summary>
		/// <param name="name">The name of the string.</param>
		/// <param name="offset">The offset of the string.</param>
		/// <param name="visible">True if the string is visible.</param>
		/// <param name="size">The size of the string in bytes.</param>
		/// <param name="pluginLine">The line in the plugin this entry is found.</param>
		void VisitAscii(string name, uint offset, bool visible, int size, uint pluginLine, string tooltip);

		/// <summary>
		///     Called when a UTF16 string is encountered in the plugin.
		/// </summary>
		/// <param name="name">The name of the string.</param>
		/// <param name="offset">The offset of the string.</param>
		/// <param name="visible">True if the string is visible.</param>
		/// <param name="size">The size of the string in bytes.</param>
		/// <param name="pluginLine">The line in the plugin this entry is found.</param>
		void VisitUtf16(string name, uint offset, bool visible, int size, uint pluginLine, string tooltip);

		/// <summary>
		///     Called when a argb32 or rgb32 is encountered in the plugin.
		/// </summary>
		/// <param name="name">The name of the color.</param>
		/// <param name="offset">The offset of the color.</param>
		/// <param name="visible">True if the color entry is visible.</param>
		/// <param name="alpha">True if alpha is used.</param>
		/// <param name="pluginLine">The line in the plugin this entry is found.</param>
		void VisitColorInt(string name, uint offset, bool visible, bool alpha, uint pluginLine, string tooltip);

		/// <summary>
		///     Called when a argbf or rgbf is encountered in the plugin.
		/// </summary>
		/// <param name="name">The name of the color.</param>
		/// <param name="offset">The offset of the color.</param>
		/// <param name="visible">True if the color entry is visible.</param>
		/// <param name="alpha">True if alpha is used.</param>
		/// <param name="basic">True if a basic color conversion is used instead of sRGB.</param>
		/// <param name="pluginLine">The line in the plugin this entry is found.</param>
		void VisitColorF(string name, uint offset, bool visible, bool alpha, bool basic, uint pluginLine, string tooltip);

		// These are called whenever flags are found in the plugin.
		// Return false from one of these methods to skip over the
		// bits in the flags.
		bool EnterFlags8(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		bool EnterFlags16(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		bool EnterFlags32(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		bool EnterFlags64(string name, uint offset, bool visible, uint pluginLine, string tooltip);

		/// <summary>
		///     Called when a bit definition is encountered inside a flags field.
		/// </summary>
		/// <param name="name">The bit's name.</param>
		/// <param name="index">The bit's zero-based index (0 = LSB).</param>
		void VisitBit(string name, int index, string tooltip);

		/// <summary>
		///     Called when a flags definition is exited.
		/// </summary>
		void LeaveFlags();

		// These are called whenever an enum is found in the plugin.
		// Return false from one of these methods to skip over the
		// options in the enum.
		bool EnterEnum8(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		bool EnterEnum16(string name, uint offset, bool visible, uint pluginLine, string tooltip);
		bool EnterEnum32(string name, uint offset, bool visible, uint pluginLine, string tooltip);

		/// <summary>
		///     Called when an enum option definition is encountered inside an enum.
		/// </summary>
		/// <param name="name">The option's name.</param>
		/// <param name="value">The option's value.</param>
		void VisitOption(string name, int value, string tooltip);

		/// <summary>
		///     Called when an enum definition is exited.
		/// </summary>
		void LeaveEnum();

		/// <summary>
		///     Called when a tag block definition is entered.
		/// </summary>
		/// <param name="name">The block's name.</param>
		/// <param name="offset">The offset of the block's size and pointer.</param>
		/// <param name="visible">True if the block is visible.</param>
		/// <param name="elementSize">The size of each element in the block.</param>
		/// <param name="align">The power of two to align the block on.</param>
		/// <param name="sort">Whether or not this block needs sorting.</param>
		/// <param name="pluginLine">The line in the plugin this entry is found.</param>
		/// <returns>False if the entries in the block should be skipped over.</returns>
		bool EnterTagBlock(string name, uint offset, bool visible, uint entrySize, int align, bool sort, uint pluginLine, string tooltip);

		/// <summary>
		///     Called when a tag block definition is exited.
		/// </summary>
		void LeaveTagBlock();

		/// <summary>
		/// Called when a shader reference is encountered in the plugin.
		/// </summary>
		/// <param name="name">The shader's name.</param>
		/// <param name="offset">The shader's offset.</param>
		/// <param name="visible">True if the shader is visible.</param>
		/// <param name="type">The shader's type.</param>
		/// <param name="pluginLine">The line in the plugin this entry is found.</param>
		void VisitShader(string name, uint offset, bool visible, ShaderType type, uint pluginLine, string tooltip);

		/// <summary>
		/// Called when a multilingual unicode string list is encountered in the plugin.
		/// </summary>
		/// <param name="name">The list's name.</param>
		/// <param name="offset">The list's offset.</param>
		/// <param name="visible">True if the list is visible.</param>
		/// <param name="languages">The number of languages in the list.</param>
		/// <param name="pluginLine">The line in the plugin this entry is found.</param>
		void VisitUnicList(string name, uint offset, bool visible, int languages, uint pluginLine, string tooltip);
	}
}