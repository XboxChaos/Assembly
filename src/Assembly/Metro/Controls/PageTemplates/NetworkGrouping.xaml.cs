using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Assembly.Helpers.Net;
using Assembly.Helpers.UIX;
using Assembly.Metro.Dialogs;

namespace Assembly.Metro.Controls.PageTemplates
{
	/// <summary>
	///     Interaction logic for NetworkGrouping.xaml
	/// </summary>
	public partial class NetworkGrouping : UserControl
	{
		public NetworkGrouping()
		{
			InitializeComponent();

			gridNetworkType.Visibility = gridSignedIn.Visibility = Visibility.Collapsed;
			gridSignIn.Visibility = Visibility.Visible;
		}

		private void btnSignIn_Click(object sender, RoutedEventArgs e)
		{
			var credentials = new UserCredentials();
			credentials.Username = txtAccountUsername.Text;
			credentials.Password = txtAccountPassword.Password;

			var thrd = new Thread(SignInThread);
			thrd.SetApartmentState(ApartmentState.STA);
			thrd.Start(credentials);
		}

		private void SignInThread(object credentialsObj)
		{
			// Try to Sign In
			try
			{
				var credentials = (UserCredentials) credentialsObj;

				// Send Signin Package to Server        
				SignInResponse response = SignIn.AttemptSignIn(credentials.Username, credentials.Password);
				if (response == null || !response.Successful)
				{
					Dispatcher.Invoke(new Action(delegate { MetroMessageBox.Show("Unable to Sign In", response.ErrorMessage); }));
					return;
				}

				// Set the UI to show that we have signed in successfully
				Dispatcher.Invoke(new Action(delegate
				{
					gridNetworkType.Visibility = gridSignedIn.Visibility = Visibility.Visible;
					gridSignIn.Visibility = Visibility.Collapsed;
					lblSignedInWelcome.Text = response.DisplayName.ToLower();
					lblSignedInPosts.Text = string.Format("posts: {0:##,###}", response.PostCount);

					// Validate Avatar
					if (!string.IsNullOrEmpty(response.AvatarUrl) && response.AvatarUrl != "http://uploads.xbchaos.netdna-cdn.com/")
						ImageLoader.LoadImageAndFade(imgSignedInAvatar, new Uri(response.AvatarUrl), new AnimationHelper(this));

					MetroMessageBox.Show("welcome", "Welcome to network poking, " + response.DisplayName);
				}));
			}
			catch (Exception ex)
			{
				Dispatcher.Invoke(new Action(delegate { MetroException.Show(ex); }));
			}
		}

		public bool Close()
		{
			// Remove all session data, then close this mofo

			return true;
		}

		private class UserCredentials
		{
			public string Username { get; set; }
			public string Password { get; set; }
		}
	}
}