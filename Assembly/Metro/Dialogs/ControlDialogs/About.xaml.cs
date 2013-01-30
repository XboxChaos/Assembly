using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Assembly.Helpers;
using Assembly.Metro.Native;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About
    {
        public About()
        {
            InitializeComponent();
            DwmDropShadow.DropShadowToWindow(this);
        }

        private void headerThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Left = Left + e.HorizontalChange;
            Top = Top + e.VerticalChange;
        }
        private void btnActionClose_Click(object sender, RoutedEventArgs e) { Close(); }
        private void lblZedd_MouseDown(object sender, MouseButtonEventArgs e)
        {
	        imageOfGodOfAllThingsHolyAndModdy.Visibility = _0xabad1dea.GodOfAllThingsHolyAndModdy.ShowGod(); 
			lblZedd.Text = _0xabad1dea.GodOfAllThingsHolyAndModdy.ShowGodsRealName();
        }
    }
}
