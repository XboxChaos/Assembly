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

			double w = (c1 * c2 * c3 - s1 * s2 * s3);
			double i = (c1 * c2 * s3 + s1 * s2 * c3);
			double j = (s1 * c2 * c3 + c1 * s2 * s3);
			double k = (c1 * s2 * c3 - s1 * c2 * s3);

			if (DataContext.GetType() == typeof(Vector4Data))
			{
				Vector4Data field = (Vector4Data)DataContext;
				field.D = (float)w;
				field.A = (float)i;
				field.B = (float)j;
				field.C = (float)k;
			}
			else if (DataContext.GetType() == typeof(Quaternion16Data))
			{
				Quaternion16Data field = (Quaternion16Data)DataContext;
				field.D = (short)(w * short.MaxValue);
				field.A = (short)(i * short.MaxValue);
				field.B = (short)(j * short.MaxValue);
				field.C = (short)(k * short.MaxValue);
			}
		}

		private void TxtConv_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!(bool)btnOptions.IsChecked || converterOpening)
				return;

			ConvertToQuat();
		}

		private void ConvertToAngle()
		{
			double w;
			double i;
			double j;
			double k;

			if (DataContext.GetType() == typeof(Vector4Data))
			{
				Vector4Data field = (Vector4Data)DataContext;
				w = field.D;
				i = field.A;
				j = field.B;
				k = field.C;
			}
			else if (DataContext.GetType() == typeof(Quaternion16Data))
			{
				Quaternion16Data field = (Quaternion16Data)DataContext;
				w = field.D;
				i = field.A;
				j = field.B;
				k = field.C;

				w = w / short.MaxValue * 100000 / 100000;
				i = i / short.MaxValue * 100000 / 100000;
				j = j / short.MaxValue * 100000 / 100000;
				k = k / short.MaxValue * 100000 / 100000;
			}
			else return;

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