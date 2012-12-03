using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Assembly.Backend.UIX
{
    public class AnimationHelper
    {
        private FrameworkElement _environment;

        public AnimationHelper(FrameworkElement environment)
        {
            _environment = environment;
        }

        public void FadeIn(FrameworkElement controlToFade)
        {
            FadeIn(controlToFade, 1000);
        }

        public void FadeIn(FrameworkElement controlToFade, int speedInMS)
        {
            controlToFade.Visibility = Visibility.Visible;

            Storyboard storyboard = new Storyboard();
            TimeSpan duration = new TimeSpan(0, 0, 0, 0, speedInMS);

            DoubleAnimation animation = new DoubleAnimation();
            animation.From = 0.0;
            animation.To = 1.0;
            animation.Duration = new Duration(duration);
            Storyboard.SetTarget(animation, controlToFade);
            Storyboard.SetTargetProperty(animation, new PropertyPath(Control.OpacityProperty));
            storyboard.Children.Add(animation);
            storyboard.Begin(_environment);
        }

        public void FadeOut(FrameworkElement controlToFade)
        {
            FadeOut(controlToFade, 250);
        }

        public void FadeOut(FrameworkElement controlToFade, int speedInMS)
        {
            Storyboard storyboard = new Storyboard();
            TimeSpan duration = new TimeSpan(0, 0, 0, 0, speedInMS);

            DoubleAnimation animation = new DoubleAnimation();
            animation.From = 1.0;
            animation.To = 0.0;
            animation.Duration = new Duration(duration);
            Storyboard.SetTarget(animation, controlToFade);
            Storyboard.SetTargetProperty(animation, new PropertyPath(Control.OpacityProperty));
            storyboard.Children.Add(animation);

            storyboard.Completed += (o, args) =>
            {
                controlToFade.Visibility = Visibility.Collapsed;
            };

            storyboard.Begin(_environment);
        }
    }
}
