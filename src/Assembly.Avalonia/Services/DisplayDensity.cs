using System;
using Avalonia;

namespace Assembly.Avalonia.Services
{
	/// <summary>
	///     The three density levels a user can pick between, and the single place that turns a
	///     level into concrete geometry. "Compact" and "Comfortable" are not Default scaled by a
	///     constant factor - each dimension (type scale, spacing scale, control height, row
	///     padding) was chosen by eye against the field table with a 503-field tag open, so the
	///     three tiers stay internally coherent (line-height still clears the font size, hit
	///     targets stay clickable) instead of degenerating into "smaller numbers" at the compact
	///     end. See MetroTypography.axaml's header for the type/spacing scale this extends, and
	///     MetroStyles.axaml for where the resource keys below are consumed.
	/// </summary>
	public enum DensityLevel
	{
		Compact,
		Default,
		Comfortable
	}

	/// <summary>
	///     Applies a <see cref="DensityLevel" /> to the running application by overwriting the
	///     type-scale, spacing-scale and control-geometry resource keys that MetroTypography.axaml
	///     and MetroStyles.axaml declare as design-time (Default-tier) fallback values.
	/// </summary>
	/// <remarks>
	///     This mutates <see cref="Application.Resources" /> directly rather than swapping a
	///     merged <c>ResourceDictionary</c> in and out, on the same principle Converters.cs's
	///     <c>OkBrush</c>/<c>DirtyBrush</c> keys already document: an application-level resource
	///     dictionary's own local entries shadow a same-named key arriving through a merged
	///     dictionary (MetroTypography.axaml; MetroStyles.axaml's <c>Styles.Resources</c>), and
	///     every consumer already reaches these keys through <c>DynamicResource</c> - the same
	///     live mechanism that already makes the dark/light theme swap without a restart (see
	///     App.axaml's header). This was verified empirically against this project's actual
	///     Avalonia 12.1.1 build rather than assumed from WPF experience, where the precedence
	///     rules differ: a plain <c>Application.Resources["Key"] = value</c> mutation does
	///     propagate live to every <c>DynamicResource</c> consumer, including one reached only
	///     through a Style Setter - but a Style Setter, even one gated by a pseudo-class such as
	///     <c>:checked</c>, can never override a property that already carries a local value
	///     (literal, <c>{StaticResource}</c>, or a <c>{DynamicResource}</c> set directly on the
	///     element rather than through a Setter - Avalonia's <c>BindingPriority.LocalValue</c>
	///     outranks <c>BindingPriority.Style</c> and <c>StyleTrigger</c> unconditionally).
	///     Concretely: field-table and tag-tree row HEIGHT (driven by
	///     <c>ListBoxItem</c>/<c>TreeViewItem</c> Padding/MinHeight, which nothing sets locally)
	///     reflows live; the row content's own inline font sizes
	///     (<c>Views/MainWindow.axaml</c>'s field-table/tag-tree <c>DataTemplate</c>s, outside
	///     this pass's ownership this wave, and literal values there regardless) do not, and
	///     changing them was not attempted here - see this pass's final report for the reasoning
	///     and for why a per-row Margin/Background driven by a value converter (the indent guides)
	///     was deliberately left density-invariant rather than half-wired to something that would
	///     only refresh on next scroll or reselect.
	/// </remarks>
	public static class DisplayDensity
	{
		/// <summary>All levels, in their natural Compact-to-Comfortable order - what a picker
		/// should enumerate and what <see cref="Next" /> steps through.</summary>
		public static readonly DensityLevel[] Levels = { DensityLevel.Compact, DensityLevel.Default, DensityLevel.Comfortable };

		public static DensityLevel Current { get; private set; } = DensityLevel.Default;

		/// <summary>Raised after <see cref="Apply" /> has finished pushing every resource for the
		/// new level - a hook for chrome (the toolbar picker, the View menu's checkmarks) that
		/// needs to reflect a density change made somewhere else (another control, a future
		/// command-palette entry) rather than the one that triggered it.</summary>
		public static event EventHandler? Changed;

		public static string Label(DensityLevel level) => level switch
		{
			DensityLevel.Compact => "Compact",
			DensityLevel.Comfortable => "Comfortable",
			_ => "Default"
		};

		/// <summary>
		///     Pushes every density-affected resource key for <paramref name="level" /> into
		///     <see cref="Application.Resources" /> and updates <see cref="Current" />; every
		///     matching <c>DynamicResource</c> consumer in the visual tree reflows on this same
		///     call, with no further nudge needed (see the type header remarks). Safe to call
		///     before a <see cref="global::Avalonia.Controls.Window" /> exists - the null-guard
		///     below matches Converters.cs's <c>ThemeBrush.Resolve</c>. Deliberately does not
		///     persist anything; <see cref="AppSettings.Density" />'s setter is the one path that
		///     both applies and saves a user's actual choice, so this method alone is also the
		///     right one for a one-run-only override (see App.axaml.cs's <c>ASM_DENSITY</c>).
		/// </summary>
		public static void Apply(DensityLevel level)
		{
			var resources = Application.Current?.Resources;
			if (resources == null) return;

			var m = MetricsFor(level);
			resources["FontSizeMicro"] = m.FontSizeMicro;
			resources["FontSizeSmall"] = m.FontSizeSmall;
			resources["FontSizeBase"] = m.FontSizeBase;
			resources["FontSizeMedium"] = m.FontSizeMedium;
			resources["FontSizeLarge"] = m.FontSizeLarge;
			resources["FontSizeTitle"] = m.FontSizeTitle;
			resources["FontSizeDisplay"] = m.FontSizeDisplay;

			resources["Space1"] = m.Space1;
			resources["Space2"] = m.Space2;
			resources["Space3"] = m.Space3;
			resources["Space4"] = m.Space4;
			resources["Space5"] = m.Space5;
			resources["Space6"] = m.Space6;

			resources["ControlHeight"] = m.ControlHeight;
			resources["ChromeToggleWidth"] = m.ChromeToggleWidth;
			resources["ListBoxItemPadding"] = m.ListBoxItemPadding;
			resources["FieldRowPadding"] = m.FieldRowPadding;
			resources["BlockExpanderSize"] = m.BlockExpanderSize;
			resources["TypeBadgePadding"] = m.TypeBadgePadding;
			resources["TreeViewItemMinHeight"] = m.TreeRowMinHeight;
			resources["TreeViewItemExpandCollapseChevronSize"] = m.TreeChevronSize;

			Current = level;
			Changed?.Invoke(null, EventArgs.Empty);
		}

		/// <summary>Steps to the next level, wrapping Comfortable back to Compact - the single
		/// action a command-palette "cycle density" entry needs.</summary>
		public static DensityLevel Next(DensityLevel level)
		{
			int i = Array.IndexOf(Levels, level);
			return Levels[(i + 1) % Levels.Length];
		}

		/// <summary>
		///     The three tiers' concrete values. "Default" is exactly MetroTypography.axaml's and
		///     MetroStyles.axaml's pre-existing literals, so selecting it - the app's own startup
		///     default - never changes anything about how the app already looked before this pass.
		///     Compact/Comfortable roughly bracket it by -/+ 10-30% depending on the dimension
		///     (control height moves less than spacing, since a sub-19px hit target stops being
		///     comfortably clickable well before a field row needs to stop breathing).
		/// </summary>
		internal static DensityMetrics MetricsFor(DensityLevel level) => level switch
		{
			DensityLevel.Compact => new DensityMetrics(
				FontSizeMicro: 9, FontSizeSmall: 10, FontSizeBase: 11, FontSizeMedium: 12, FontSizeLarge: 14, FontSizeTitle: 17, FontSizeDisplay: 24,
				Space1: 3, Space2: 6, Space3: 9, Space4: 12, Space5: 15, Space6: 18,
				ControlHeight: 19, ChromeToggleWidth: 22,
				ListBoxItemPadding: new Thickness(6, 2), FieldRowPadding: new Thickness(0, 1, 6, 1),
				BlockExpanderSize: 14, TypeBadgePadding: new Thickness(3, 0),
				TreeRowMinHeight: 17, TreeChevronSize: 8),

			DensityLevel.Comfortable => new DensityMetrics(
				FontSizeMicro: 11, FontSizeSmall: 12, FontSizeBase: 13, FontSizeMedium: 14, FontSizeLarge: 17, FontSizeTitle: 20, FontSizeDisplay: 30,
				Space1: 5, Space2: 10, Space3: 15, Space4: 20, Space5: 25, Space6: 30,
				ControlHeight: 26, ChromeToggleWidth: 30,
				ListBoxItemPadding: new Thickness(10, 5), FieldRowPadding: new Thickness(0, 6, 8, 6),
				BlockExpanderSize: 18, TypeBadgePadding: new Thickness(5, 2),
				TreeRowMinHeight: 26, TreeChevronSize: 10),

			_ => new DensityMetrics(
				FontSizeMicro: 10, FontSizeSmall: 11, FontSizeBase: 12, FontSizeMedium: 13, FontSizeLarge: 15, FontSizeTitle: 18, FontSizeDisplay: 26,
				Space1: 4, Space2: 8, Space3: 12, Space4: 16, Space5: 20, Space6: 24,
				ControlHeight: 22, ChromeToggleWidth: 26,
				ListBoxItemPadding: new Thickness(8, 3), FieldRowPadding: new Thickness(0, 3, 8, 3),
				BlockExpanderSize: 16, TypeBadgePadding: new Thickness(4, 1),
				TreeRowMinHeight: 21, TreeChevronSize: 9)
		};
	}

	/// <summary>One density tier's full set of geometry values - see <see cref="DisplayDensity.MetricsFor" />.</summary>
	internal readonly record struct DensityMetrics(
		double FontSizeMicro, double FontSizeSmall, double FontSizeBase, double FontSizeMedium, double FontSizeLarge, double FontSizeTitle, double FontSizeDisplay,
		double Space1, double Space2, double Space3, double Space4, double Space5, double Space6,
		double ControlHeight, double ChromeToggleWidth,
		Thickness ListBoxItemPadding, Thickness FieldRowPadding, double BlockExpanderSize, Thickness TypeBadgePadding,
		double TreeRowMinHeight, double TreeChevronSize);
}
