using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Plugins.Fields
{
	/// <summary>
	/// A comment in a plugin.
	/// </summary>
	public class CommentField : PluginField
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CommentField" /> class.
		/// </summary>
		/// <param name="title">The comment's title.</param>
		/// <param name="text">The comment's text.</param>
		/// <param name="id">The field's unique ID string, or <c>null</c> for none.</param>
		/// <param name="alwaysVisible">If set to <c>true</c>, the field should always be shown in an editor.</param>
		/// <param name="sourceFile">The name of the file that the field originated from, or <c>null</c> for none.</param>
		/// <param name="sourceLine">The zero-based line number that the field originated from, or a negative value for none.</param>
		public CommentField(string title, string text, string id, bool alwaysVisible, string sourceFile, int sourceLine)
			: base(id, alwaysVisible, sourceFile, sourceLine)
		{
			Title = title;
			Text = text;
		}

		/// <summary>
		/// Gets the comment's title.
		/// </summary>
		public string Title { get; private set; }

		/// <summary>
		/// Gets the comment's text.
		/// </summary>
		public string Text { get; private set; }

		public override void Accept(IPluginFieldVisitor visitor)
		{
			throw new NotImplementedException();
		}
	}
}
