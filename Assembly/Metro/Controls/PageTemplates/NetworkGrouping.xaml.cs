using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data;
using Assembly.Metro.Dialogs;
using Assembly.Backend.Cryptography;
using System.Threading;
using Assembly.Backend;
using System.Web.Script.Serialization;
using Assembly.Backend.UIX;

namespace Assembly.Metro.Controls.PageTemplates
{
    /// <summary>
    /// Interaction logic for NetworkGrouping.xaml
    /// </summary>
    public partial class NetworkGrouping : UserControl
    {
        private JavaScriptSerializer JSON = new JavaScriptSerializer();

        public NetworkGrouping()
        {
            InitializeComponent();

            gridNetworkType.Visibility = gridSignedIn.Visibility = System.Windows.Visibility.Collapsed;
            gridSignIn.Visibility = System.Windows.Visibility.Visible;
        }

        private void btnSignIn_Click(object sender, RoutedEventArgs e)
        {
            Thread thrd = new Thread(new ThreadStart(SignInThread));
            thrd.SetApartmentState(ApartmentState.STA);
            thrd.Start();
        }
        private ServerConnector.ServerError signinError = null;
        private SignInResponse _signinResponse;
        public SignInResponse SigninData { get { return _signinResponse; } }
        private void SignInThread()
        {
            // Try to Sign In
            try
            {
                SignIn signIn = new SignIn();
                Dispatcher.Invoke(new Action(delegate
                {
                    // Create JSON Query
                    signIn = new SignIn()
                    {
                        username = txtAccountUsername.Text,
                        password = MD5Crypto.ComputeHashToString(txtAccountPassword.Password).ToLower()
                    };
                }));

                // Send Signin Package to Server        
                string svrResponse = ServerConnector.SendServerAPICall(signIn);
                if (svrResponse.Contains("assembly_error_code"))
                {
                    signinError = JSON.Deserialize<ServerConnector.ServerError>(svrResponse);
                    throw new Exception();
                }
                _signinResponse = JSON.Deserialize<SignInResponse>(svrResponse);


                // Set the UI to show that we have signed in successfully
                Dispatcher.Invoke(new Action(delegate
                {
                    gridNetworkType.Visibility = gridSignedIn.Visibility = System.Windows.Visibility.Visible;
                    gridSignIn.Visibility = System.Windows.Visibility.Collapsed;
                    lblSignedInWelcome.Text = _signinResponse.display_name.ToLower();
                    lblSignedInPosts.Text = string.Format("posts: {0:##,###}", _signinResponse.post_count.ToString());

                    // Validate Avatar
                    if (_signinResponse.avatar_url != "" && _signinResponse.avatar_url != null && _signinResponse.avatar_url != "http://uploads.xbchaos.netdna-cdn.com/")
                            ImageLoader.LoadImageAndFade(imgSignedInAvatar, new Uri(_signinResponse.avatar_url), new AnimationHelper(this));

                    MetroMessageBox.Show("welcome", "Welcome to network poking, " + _signinResponse.display_name);
                }));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    if (signinError != null)
                        MetroMessageBox.Show("Unable to Sign In", signinError.error_description);
                    else
                        MetroException.Show(ex);

                    signinError = null;
                }));
            }
        }

        public bool Close()
        {
            // Remove all session data, then close this mofo

            return true;
        }

        public class SignIn
        {
            public string action = "signin";
            public string username { get; set; }
            public string password { get; set; }
        }
        public class SignInResponse
        {
            public int timestamp { get; set; }
            public int member_id { get; set; }
            public string session_id { get; set; }
            public string display_name { get; set; }
            public string signin_name { get; set; }
            public int group_id { get; set; }
            public int post_count { get; set; }
            public string avatar_url { get; set; }
        }
    }
}