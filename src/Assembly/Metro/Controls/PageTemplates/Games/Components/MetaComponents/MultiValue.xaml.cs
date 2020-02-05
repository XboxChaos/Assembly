using System.Windows.Controls;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaComponents
{
	/// <summary>
	///     Interaction logic for MultiValue.xaml
	/// </summary>
	public partial class MultiValue : UserControl
	{
		public MultiValue()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Width of non-range control with 2 values.
		/// </summary>
		public double Multi2Width
		{ get { return 517; } }
		/// <summary>
		/// Width of control with 3 values.
		/// </summary>
		public double Multi3Width
		{ get { return 639; } }
		/// <summary>
		/// Width of control with 4 values.
		/// </summary>
		public double Multi4Width
		{ get { return 761; } }

		/// <summary>
		/// Width of control with 2 values, with a gap before the final value.
		/// </summary>
		public double RangeWidth
		{ get { return 523; } }
		/// <summary>
		/// Width of control with 3 values, with a gap before the final value.
		/// </summary>
		public double Plane2Width
		{ get { return 651; } }
		/// <summary>
		/// Width of control with 4 values, with a gap before the final value.
		/// </summary>
		public double Plane3Width
		{ get { return 773; } }
	}
}