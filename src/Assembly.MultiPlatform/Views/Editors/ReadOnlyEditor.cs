using Assembly.MultiPlatform.Services;
using Avalonia.Controls;
using Avalonia.Media;

namespace Assembly.MultiPlatform.Views.Editors
{
	/// <summary>
	///     Editor for every field kind this pass does not write back (raw/hex blobs, tag and data
	///     references, datum indices, comments, and every Campaign Evolved field whose value type
	///     has no write-back API - see <see cref="MetaFieldDef.NotEditableReason" />). Read-only
	///     does not mean uninspectable: whenever the bytes can actually be re-read (any
	///     classic-engine field - see <see cref="FieldEditorContext.TryReadRawBytes" />), this shows
	///     a real hex view with an ASCII gutter rather than the field table's own truncated preview
	///     string. A Campaign Evolved field has no file offset to re-read from at all, so it falls
	///     back to the value already resolved when the tag was opened
	///     (<see cref="MetaRowViewModel.DisplayValue" />).
	/// </summary>
	public sealed class ReadOnlyEditor : FieldEditorBase
	{
		private HexView? _hex;

		protected override void OnBind()
		{
			Children.Add(new TextBlock
			{
				Text = Context.Row.Def.NotEditableReason ?? ReasonFor(Context.Row.Def.Kind),
				Classes = { "label" },
				TextWrapping = TextWrapping.Wrap
			});

			if (Context.TryReadRawBytes(out var bytes) && bytes.Length > 0)
			{
				_hex = new HexView();
				_hex.SetBytes(bytes, Context.Row.AbsoluteOffset);
				Children.Add(_hex);
			}
			else if (Context.Doc.IsFifthGen)
			{
				Children.Add(new SelectableTextBlock
				{
					Text = Context.Row.DisplayValue,
					Classes = { "mono" },
					FontSize = 11,
					TextWrapping = TextWrapping.Wrap
				});
			}
		}

		protected override void OnRowChanged(string? propertyName)
		{
			if (_hex == null) return;
			if (Context.TryReadRawBytes(out var bytes)) _hex.SetBytes(bytes, Context.Row.AbsoluteOffset);
		}

		public override string? GetClipboardValue()
		{
			if (Context.TryReadRawBytes(out var bytes) && bytes.Length > 0) return HexView.ToHexLine(bytes);
			return Context.Row.DisplayValue;
		}

		private static string ReasonFor(MetaFieldKind kind) => kind switch
		{
			MetaFieldKind.TagReference => "Tag references are shown resolved, but retargeting them isn't wired up yet.",
			MetaFieldKind.DataReference => "Raw data references (variable-length blobs) aren't editable yet - they need the allocator.",
			MetaFieldKind.RawData or MetaFieldKind.HexString => "Raw/hex byte blobs aren't editable yet.",
			MetaFieldKind.Datum => "Datum indices are identifiers, not editable values.",
			MetaFieldKind.Comment => "This is an informational note, not a field.",
			// Def.NotEditableReason (set by TagDocumentViewModel.GetFifthGenScalarDef) always wins
			// for an actual FifthGenValue row - see OnBind above - so reaching this case at all
			// would mean that reason was somehow null; kept as a defensive fallback rather than
			// a real day-to-day message.
			MetaFieldKind.FifthGenValue =>
				"This Campaign Evolved field's value type has no write-back API in Blamite yet.",
			_ => $"{kind} fields are read-only in this pass."
		};
	}
}
