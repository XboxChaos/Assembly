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
using Assembly.Helpers.Cryptography;
using System.Threading;
using Assembly.Helpers;
using System.Web.Script.Serialization;
using Assembly.Helpers.UIX;
using System.Runtime.Serialization;
using Assembly.Helpers.Net;

namespace Assembly.Metro.Controls.PageTemplates
{
    /// <summary>
    /// Interaction logic for NetworkGrouping.xaml
    /// </summary>
    public partial class NetworkGrouping : UserControl
    {
        class UserCredentials
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        public NetworkGrouping()
        {
            InitializeComponent();

            gridNetworkType.Visibility = gridSignedIn.Visibility = System.Windows.Visibility.Collapsed;
            gridSignIn.Visibility = System.Windows.Visibility.Visible;
        }

        private void btnSignIn_Click(object sender, RoutedEventArgs e)
        {
            UserCredentials credentials = new UserCredentials();
            credentials.Username = txtAccountUsername.Text;
            credentials.Password = txtAccountPassword.Password;

            Thread thrd = new Thread(new ParameterizedThreadStart(SignInThread));
            thrd.SetApartmentState(ApartmentState.STA);
            thrd.Start(credentials);
        }

        private void SignInThread(object credentialsObj)
        {
            // Try to Sign In
            try
            {
                UserCredentials credentials = (UserCredentials)credentialsObj;

                // Send Signin Package to Server        
                SignInResponse response = SignIn.AttemptSignIn(credentials.Username, credentials.Password);
                if (response == null || !response.Successful)
                {
                    Dispatcher.Invoke(new Action(delegate
                        {
                            MetroMessageBox.Show("Unable to Sign In", response.ErrorMessage);
                        }));
                    return;
                }

                // Set the UI to show that we have signed in successfully
                Dispatcher.Invoke(new Action(delegate
                {
                    gridNetworkType.Visibility = gridSignedIn.Visibility = System.Windows.Visibility.Visible;
                    gridSignIn.Visibility = System.Windows.Visibility.Collapsed;
                    lblSignedInWelcome.Text = response.DisplayName.ToLower();
                    lblSignedInPosts.Text = string.Format("posts: {0:##,###}", response.PostCount.ToString());

                    // Validate Avatar
                    if (!string.IsNullOrEmpty(response.AvatarUrl) && response.AvatarUrl != "http://uploads.xbchaos.netdna-cdn.com/")
                            ImageLoader.LoadImageAndFade(imgSignedInAvatar, new Uri(response.AvatarUrl), new AnimationHelper(this));

                    MetroMessageBox.Show("welcome", "Welcome to network poking, " + response.DisplayName);
                }));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    MetroException.Show(ex);
                }));
            }
        }

        public bool Close()
        {
            // Remove all session data, then close this mofo

            return true;
        }
    }
}