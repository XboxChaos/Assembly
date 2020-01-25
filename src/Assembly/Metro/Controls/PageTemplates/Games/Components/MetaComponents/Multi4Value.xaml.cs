using System;
using System.Windows.Controls;
using Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaComponents
{
	/// <summary>
	///     Interaction logic for Multi4Value.xaml
	/// </summary>
	public partial class Multi4Value : UserControl
	{
		bool converterOpening = false;

		public Multi4Value()
		{
			InitializeComponent();
		}

		private void ConvertToQuat()
		{
			if (DataContext.GetType() != typeof(Vector4Data))
				return;

			double yaw;
			double pitch;
			double roll;

			if (!double.TryParse(txtConvY.Text, out yaw))
				return;
			if (!double.TryParse(txtConvP.Text, out pitch))
				return;
			if (!double.TryParse(txtConvR.Text, out roll))
				return;

			yaw *= (Math.PI / 180);
			pitch *= (Math.PI / 180);
			roll *= (Math.PI / 180);

			// http://www.euclideanspace.com/maths/geometry/rotations/conversions/eulerToQuaternion/
			double c1 = Math.Cos(yaw / 2);
			double s1 = Math.Sin(yaw / 2);

			double c2 = Math.Cos(pitch / 2);
			double s2 = Math.Sin(pitch / 2);

			double c3 = Math.Cos(roll / 2);
			double s3 = Math.Sin(roll / 2);

			Vector4Data field = (Vector4Data)DataContext;
			field.D = (float)(c1 * c2 * c3 - s1 * s2 * s3);
			field.A = (float)(c1 * c2 * s3 + s1 * s2 * c3);
			field.B = (float)(s1 * c2 * c3 + c1 * s2 * s3);
			field.C = (float)(c1 * s2 * c3 - s1 * c2 * s3);
		}

		private void TxtConv_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!(bool)btnOptions.IsChecked || converterOpening)
				return;

			ConvertToQuat();
		}

		private void ConvertToAngle()
		{
			if (DataContext.GetType() != typeof(Vector4Data))
				return;

			Vector4Data field = (Vector4Data)DataContext;
			double w = field.D;
			double i = field.A;
			double j = field.B;
			double k = field.C;

			// http://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToEuler/
			double yaw;
			double pitch;
			double roll;

			double sqw = w * w;
			double sqi = i * i;
			double sqj = j * j;
			double sqk = k * k;

			double unit = sqi + sqj + sqk + sqw; // if normalised is one, otherwise is correction factor
			double test = i * j + k * w;

			if (test > 0.4999 * unit)
			{ // singularity at north pole
				yaw = 2 * Math.Atan2(i, w);
				pitch = Math.PI / 2;
				roll = 0;
			}
			if (test < -0.4999 * unit)
			{ // singularity at south pole
				yaw = -2 * Math.Atan2(i, w);
				pitch = -Math.PI / 2;
				roll = 0;
			}
			else
			{
				yaw = Math.Atan2(2 * j * w - 2 * i * k, sqi - sqj - sqk + sqw);
				pitch = Math.Asin(2 * test / unit);
				roll = Math.Atan2(2 * i * w - 2 * j * k, -sqi + sqj - sqk + sqw);
			}
	
			yaw *= (180 / Math.PI);
			pitch *= (180 / Math.PI);
			roll *= (180 / Math.PI);

			txtConvY.Text = yaw.ToString();
			txtConvP.Text = pitch.ToString();
			txtConvR.Text = roll.ToString();
		}

		private void BtnOptions_Checked(object sender, System.Windows.RoutedEventArgs e)
		{
			converterOpening = true;
			ConvertToAngle();
			converterOpening = false;
		}
	}
}