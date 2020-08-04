using System;
using System.Windows.Media.Imaging;
using Blamite.Blam.Scripting;


namespace Assembly.Helpers.CodeCompletion.Scripting
{
    public enum GlobalType
    {
        Map,
        Engine
    }

    public class GlobalCompletion : CompletionBase
    {
        public GlobalCompletion(string name, string valueType, GlobalType type)
        {
            Text = name;
            if(type == GlobalType.Engine)
            {
                Description = $"[Engine_GLOBAL] | Value Type: {valueType}";
            }
            else
            {
                Description = $"[GLOBAL] | Value Type: {valueType}";
            }
            Priority = 1.0;
            var image = new BitmapImage(new Uri("pack://application:,,,/Metro/Images/Field_16x.png"));
            image.Freeze();
            Image = image;
        }

        public GlobalCompletion(GlobalInfo info, GlobalType type)
        {
            Text = info.Name;
            if (type == GlobalType.Engine)
            {
                Description = $"[Engine_GLOBAL] | Value Type: {info.ReturnType}";
            }
            else
            {
                Description = $"[GLOBAL] | Value Type: {info.ReturnType}";
            }
            Priority = 1.0;
            var image = new BitmapImage(new Uri("pack://application:,,,/Metro/Images/Field_16x.png"));
            image.Freeze();
            Image = image;
        }
    }
}
