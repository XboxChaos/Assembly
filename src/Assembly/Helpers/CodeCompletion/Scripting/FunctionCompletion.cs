using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blamite.Blam.Scripting;
using System.Windows.Media.Imaging;

namespace Assembly.Helpers.CodeCompletion.Scripting
{
    public class FunctionCompletion : CompletionBase
    {
        public FunctionCompletion(FunctionInfo info)
        {
            Text = info.Name;
            StringBuilder description = new StringBuilder();
            description.Append($"[FUNCTION] | Type: {info.ReturnType}");
            if (info.ParameterTypes.Length > 0)
            {
                description.Append(" | Parameters: ");
                description.Append(string.Join(", ", info.ParameterTypes));
            }
            Description = description.ToString();
            Priority = 1.0;
            var image = new BitmapImage(new Uri("pack://application:,,,/Metro/Images/Method_16x.png"));
            image.Freeze();
            Image = image;
        }
    }
}
