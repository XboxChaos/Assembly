using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Blamite.Plugins.Fields
{
	public enum ColorFieldChannelType
	{
		Bytes,
		Floats
	}

	public enum ColorFieldChannel
	{
		Red,
		Green,
		Blue,
		Alpha
	}

	public class ColorField : ValuePluginField
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ColorField" /> class.
		/// </summary>
		/// <param name="channelType">The type of each channel in the color.</param>
		/// <param name="channels">The channels of the color, in the order that they appear in the tag data.</param>
		/// <param name="displayName">A display-friendly name for the field.</param>
		/// <param name="offset">The offset of the field from the start of its block.</param>
		/// <param name="id">The field's unique ID string.</param>
		/// <param name="alwaysVisible">If set to <c>true</c>, the field should always be shown in an editor.</param>
		/// <param name="sourceFile">The name of the file that the field originated from, or <c>null</c> for none.</param>
		/// <param name="sourceLine">The zero-based line number that the field originated from, or a negative value for none.</param>
		public ColorField(ColorFieldChannelType channelType, IEnumerable<ColorFieldChannel> channels, string displayName, uint offset, string id, bool alwaysVisible, string sourceFile, int sourceLine)
			: base(displayName, offset, id, alwaysVisible, sourceFile, sourceLine)
		{
			ChannelType = channelType;
			Channels = new ReadOnlyCollection<ColorFieldChannel>(channels.ToList());
		}

		/// <summary>
		/// Gets the type of each channel in the color.
		/// </summary>
		public ColorFieldChannelType ChannelType { get; private set; }

		/// <summary>
		/// Gets the channels of the color, in the order that they appear in the tag data.
		/// </summary>
		public ReadOnlyCollection<ColorFieldChannel> Channels { get; private set; }

		public override void Accept(IPluginFieldVisitor visitor)
		{
			throw new NotImplementedException();
		}
	}
}
