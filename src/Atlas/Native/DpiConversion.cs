using System.Windows;
using System.Windows.Forms;

namespace Atlas.Native
{
    internal static class DpiConversion
    {
        public enum Direction
        {
            Vertical,
            Horizontal
        }

        public static int PointsToPixels(double wpfPoints, Direction direction)
        {
            if (direction == Direction.Horizontal)
            {
                return (int) (wpfPoints * Screen.PrimaryScreen.WorkingArea.Width / SystemParameters.WorkArea.Width);
            }
            return (int) (wpfPoints * Screen.PrimaryScreen.WorkingArea.Height / SystemParameters.WorkArea.Height);
        }

        public static double PixelsToPoints(int pixels, Direction direction)
        {
            if (direction == Direction.Horizontal)
            {
                return pixels * SystemParameters.WorkArea.Width / Screen.PrimaryScreen.WorkingArea.Width;
            }
            return pixels * SystemParameters.WorkArea.Height / Screen.PrimaryScreen.WorkingArea.Height;
        }
    }
}
