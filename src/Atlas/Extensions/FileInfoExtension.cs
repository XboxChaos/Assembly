using System.IO;

namespace Atlas.Extensions
{
	public static class FileInfoExtension
	{
		public static bool IsLocked(this FileInfo fileInfo)
		{
			FileStream stream = null;

			try
			{
				stream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.None);
			}
			catch (IOException)
			{
				return true;
			}
			finally
			{
				if (stream != null)
					stream.Close();
			}

			return false;
		}
	}
}
