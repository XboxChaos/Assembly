using System;
using System.Windows.Media.Imaging;
using Blamite.Blam.Scripting.Context;
using Blamite.Blam.Scripting;
using System.Runtime.CompilerServices;

namespace Assembly.Helpers.CodeCompletion.Scripting
{
    public class ObjectCompletion : CompletionBase
    {
        public ObjectCompletion(ScriptingContextObject obj)
        {
            Text = obj.Name;
            Description = $"[{obj.ObjectGroup.ToUpperInvariant()}]";
            if (obj.IsChild)
            {
                Priority = 0.2;
            }
            else
            {
                Priority = 0.8;
            }
            var image = new BitmapImage(new Uri("pack://application:,,,/Metro/Images/Object_16x.png"));
            image.Freeze();
            Image = image;
        }

        public ObjectCompletion(UnitSeatMapping mapping)
        {
            Text = mapping.Name;
            Description = "[UNIT_SEAT_MAPPING]";
            Priority = 0.8;
            var image = new BitmapImage(new Uri("pack://application:,,,/Metro/Images/Object_16x.png"));
            image.Freeze();
            Image = image;
        }
    }
}
