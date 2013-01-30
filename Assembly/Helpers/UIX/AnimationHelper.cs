using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace Assembly.Helpers.UIX
{
    public class AnimationHelper
    {
        private readonly FrameworkElement _environment;

        public AnimationHelper(FrameworkElement environment)
        {
            _environment = environment;
        }

        public void FadeIn(FrameworkElement controlToFade)
        {
            FadeIn(controlToFade, 1000);
        }

        public void FadeIn(FrameworkElement controlToFade, int speedInMs)
        {
            controlToFade.Visibility = Visibility.Visible;

            var storyboard = new Storyboard();
			var duration = new TimeSpan(0, 0, 0, 0, speedInMs);

			var animation = new DoubleAnimation
				                {
					                From = 0.0, 
									To = 1.0, 
									Duration = new Duration(duration)
				                };
	        Storyboard.SetTarget(animation, controlToFade);
            Storyboard.SetTargetProperty(animation, new PropertyPath(UIElement.OpacityProperty));
            storyboard.Children.Add(animation);
            storyboard.Begin(_environment);
        }

        public void FadeOut(FrameworkElement controlToFade)
        {
            FadeOut(controlToFade, 250);
        }

        public void FadeOut(FrameworkElement controlToFade, int speedInMs)
        {
			var storyboard = new Storyboard();
			var duration = new TimeSpan(0, 0, 0, 0, speedInMs);

			var animation = new DoubleAnimation
				                {
					                From = 1.0, 
									To = 0.0, 
									Duration = new Duration(duration)
				                };
	        Storyboard.SetTarget(animation, controlToFade);
            Storyboard.SetTargetProperty(animation, new PropertyPath(UIElement.OpacityProperty));
            storyboard.Children.Add(animation);

            storyboard.Completed += (o, args) =>
            {
                controlToFade.Visibility = Visibility.Collapsed;
            };

            storyboard.Begin(_environment);
        }
    }
}
