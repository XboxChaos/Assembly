using System;
using System.Windows.Media.Imaging;

namespace Assembly.Helpers.CodeCompletion.Scripting
{
    public class ScriptCompletion : CompletionBase
    {
        public ScriptCompletion(string name, string scriptType, string returnType)
        {
            Text = name;
            Description = $"[SCRIPT] | Script Type: {scriptType} | Return Type: {returnType}";
            Priority = 1.0;
            var image = new BitmapImage(new Uri("pack://application:,,,/Metro/Images/Script_16x.png"));
            image.Freeze();
            Image = image;
        }
    }
}
