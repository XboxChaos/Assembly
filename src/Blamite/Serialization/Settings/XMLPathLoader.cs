using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Serialization.Settings
{
    public class XMLPathLoader : IComplexSettingLoader
    {
        public object LoadSetting(string path)
        {
            if(File.Exists(path))
            {
                //throw new ArgumentException("The path to a setting doesn't exist or is invalid.");
            }
            return path;
        }
    }
}
