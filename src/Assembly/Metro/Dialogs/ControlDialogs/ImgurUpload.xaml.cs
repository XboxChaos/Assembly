using System.Windows;
using System.Windows.Controls.Primitives;
using Assembly.Metro.Native;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
    /// <summary>
    /// Interaction logic for UpCapUpload.xaml
    /// </summary>
    public partial class ImgurUpload
    {
        private readonly string _uploadString;

        public ImgurUpload(string uploadString)
        {
            InitializeComponent();
            DwmDropShadow.DropShadowToWindow(this);
            _uploadString = uploadString;

            lblFriendlyLink.Text = string.Format("[url='http://imgur.com/{0}'][img]http://i.imgur.com/{0}.jpg[/img][/url]", _uploadString);
            lblEvilLink.Text = string.Format("[img]http://i.imgur.com/{0}.jpg[/img]", _uploadString);
            lblViewLink.Text = string.Format("http://imgur.com/{0}", _uploadString);
            lblDirectLink.Text = string.Format("http://i.imgur.com/{0}.jpg", _uploadString);
        }

        private void btnOkay_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
