using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assembly.Metro;
using Blamite.Blam.Scripting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.CodeCompletion;
using Blamite.Blam.Scripting.Context;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Threading;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.Editors
{
    public class ScriptCompletion : ICompletionData
    {
        public ScriptCompletion(FunctionInfo info)
        {
            Text = info.Name;
            StringBuilder description = new StringBuilder();
            description.Append($"[FUNCTION] | Type: {info.ReturnType}");
            if(info.ParameterTypes.Length > 0)
            {
                description.Append(" | Parameters: ");
                description.Append(string.Join(", ", info.ParameterTypes));
            }
            Description = description.ToString();
            Priority = 1.0;
            Image = BitmapToSource(new Icon(SystemIcons.Information, 16, 16).ToBitmap());
        }

        public ScriptCompletion(GlobalInfo info)
        {
            Text = info.Name;
            Description = $"[ENGINE_GLOBAL] | Type: {info.ReturnType}";
            Priority = 1.0;
            Image = BitmapToSource(new Icon(SystemIcons.Shield, 16, 16).ToBitmap());
        }

        public ScriptCompletion(ScriptingContextObject obj)
        {
            Text = obj.Name;
            Description = $"[{obj.ObjectGroup.ToUpper()}]";
            if(obj.IsChild)
            {
                Priority = 0.2;
            }
            else
            {
                Priority = 1.0;
            }
            Image = BitmapToSource(new Icon(SystemIcons.WinLogo, 16, 16).ToBitmap());
        }

        public ScriptCompletion(UnitSeatMapping mapping)
        {
            Text = mapping.Name;
            Description = "[UNIT_SEAT_MAPPING]";
            Priority = 1.0;
            Image = BitmapToSource(new Icon(SystemIcons.WinLogo, 16, 16).ToBitmap());
        }

        public System.Windows.Media.ImageSource Image
        {
            get; private set;
        }

        public string Text { get; private set; }

        public object Content
        {
            get { return Text; }
        }

        public object Description { get; private set; }

        public double Priority { get; private set; }

        public void Complete(TextArea textArea, ISegment completionSegment,
            EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, Text);
        }

        // https://stackoverflow.com/a/30729291
        private BitmapSource BitmapToSource(System.Drawing.Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }
    }
}
