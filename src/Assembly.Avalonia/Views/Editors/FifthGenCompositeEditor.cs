using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Blamite.Blam.FifthGen.Structures;

namespace Assembly.Avalonia.Views.Editors
{
	/// <summary>
	///     Read-only structured display for a Campaign Evolved composite real field - a point,
	///     vector, Euler pair/triple, plane, quaternion, bounds pair, packed or float colour, or
	///     rectangle. <see cref="ReadOnlyEditor" /> would otherwise show these as the field table's
	///     own single formatted string; now that <c>FifthGenTagDataReader</c> decodes them into
	///     named components (see <c>FifthGenCompositeValue.cs</c>), this shows one labelled row per
	///     component instead - a real, if read-only, component view - plus a swatch for a packed
	///     byte colour, the one composite shape whose channel meaning is unambiguous enough to paint
	///     (see the remarks on why a float colour deliberately gets none).
	/// </summary>
	/// <remarks>
	///     Still read-only: <see cref="Assembly.Avalonia.Services.MetaFieldDef.NotEditableReason" />
	///     says why for the specific value type reached, and that reason is always shown first, same
	///     as <see cref="ReadOnlyEditor" />. This class exists only to make the "what is this value"
	///     half of the sidebar honest about shape without pretending to make the "change it" half
	///     work - see this pass's report for why Blamite has no setter for any of these types yet.
	/// </remarks>
	public sealed class FifthGenCompositeEditor : FieldEditorBase
	{
		protected override void OnBind()
		{
			Children.Add(new TextBlock
			{
				Text = Context.Row.Def.NotEditableReason ?? "This field has no write-back API yet.",
				Classes = { "label" },
				TextWrapping = TextWrapping.Wrap
			});
			Children.Add(EditorVisuals.Separator());

			switch (Context.Row.FifthGenSource)
			{
				case FifthGenVectorValue v:
					AddComponents(v.Components.Select(F).ToArray());
					break;

				case FifthGenBoundsValue b:
					AddComponents(new[] { F(b.Lo), F(b.Hi) }, new[] { "Low", "High" });
					break;

				case FifthGenIntegerBoundsValue ib:
					AddComponents(
						new[] { ib.Lo.ToString(CultureInfo.InvariantCulture), ib.Hi.ToString(CultureInfo.InvariantCulture) },
						new[] { "Low", "High" });
					break;

				case FifthGenColorValue c:
					AddSwatch(c.A, c.R, c.G, c.B);
					AddComponents(
						c.HasAlpha ? new[] { c.A.ToString(CultureInfo.InvariantCulture), c.R.ToString(CultureInfo.InvariantCulture), c.G.ToString(CultureInfo.InvariantCulture), c.B.ToString(CultureInfo.InvariantCulture) }
						           : new[] { c.R.ToString(CultureInfo.InvariantCulture), c.G.ToString(CultureInfo.InvariantCulture), c.B.ToString(CultureInfo.InvariantCulture) },
						c.HasAlpha ? new[] { "A", "R", "G", "B" } : new[] { "R", "G", "B" });
					break;

				case FifthGenRealColorValue rc:
					// No swatch here - see FifthGenValueFormatter.FormatPackedColor's remarks on why
					// a float colour's components are not assumed to be normalized to 0..1; painting
					// one anyway would misrepresent an HDR value the same way that formatter avoids.
					AddComponents(rc.Components.Select(F).ToArray(), rc.HasAlpha ? new[] { "A", "R", "G", "B" } : new[] { "R", "G", "B" });
					break;

				case FifthGenRectangleValue r:
					AddComponents(r.Components.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray());
					break;

				default:
					Children.Add(new SelectableTextBlock
					{
						Text = Context.Row.DisplayValue,
						Classes = { "mono" },
						FontSize = 11,
						TextWrapping = TextWrapping.Wrap
					});
					break;
			}
		}

		/// <summary>Numbered component labels ("0", "1", ...) rather than an inferred axis meaning
		/// (x/y/z, yaw/pitch/roll) - the field's own declared type name, already shown in the header
		/// above this editor, is the authority on what each component represents; duplicating that
		/// as a guessed label here would risk asserting more than the bytes actually say.</summary>
		private void AddComponents(string[] values) =>
			AddComponents(values, Enumerable.Range(0, values.Length).Select(i => i.ToString(CultureInfo.InvariantCulture)).ToArray());

		private void AddComponents(string[] values, string[] labels)
		{
			var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14 };
			for (int i = 0; i < values.Length; i++)
			{
				var col = new StackPanel { Spacing = 2 };
				col.Children.Add(new TextBlock { Text = labels[i], Classes = { "label" }, FontSize = 10.5 });
				col.Children.Add(new SelectableTextBlock { Text = values[i], Classes = { "mono" }, FontSize = 12 });
				row.Children.Add(col);
			}
			Children.Add(row);
		}

		private void AddSwatch(byte a, byte r, byte g, byte b)
		{
			var swatch = new Border
			{
				Width = 48,
				Height = 32,
				BorderBrush = Brushes.Gray,
				BorderThickness = new Thickness(1),
				Background = new SolidColorBrush(Color.FromArgb(255, r, g, b))
			};
			Children.Add(swatch);
		}

		public override string? GetClipboardValue() => Context.Row.DisplayValue;

		private static string F(float f) => f.ToString("0.######", CultureInfo.InvariantCulture);
	}
}
