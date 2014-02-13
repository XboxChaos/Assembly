using System.Windows.Controls;

namespace Atlas.Views.Cache.TagEditorComponents
{
	/// <summary>
	///     Interaction logic for TrackBar.xaml
	/// </summary>
	public partial class TrackBar : UserControl
	{
		private double large_change;
		private double maximum;
		private double minimum;
		private double small_change;

		public TrackBar()
		{
			InitializeComponent();
		}

		public void LoadValues(string title, string type, double min, double max, double smallStep, double largeStep)
		{
			lblName.ToolTip = lblName.Text = title;
			minimum = min;
			maximum = max;
			small_change = smallStep;
			large_change = largeStep;
		}
	}
}