using System.IO;

namespace Blamite.Serialization.Settings
{
    public class XMLSeatMappingLoader: IComplexSettingLoader
    {
        public object LoadSetting(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }
    }
}
