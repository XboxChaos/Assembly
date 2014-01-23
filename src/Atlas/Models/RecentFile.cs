using System.IO;
using Atlas.ViewModels;

namespace Atlas.Models
{
	public class RecentFile
	{
		public RecentFile() { }

		public RecentFile(string filePath, HomeViewModel.Type type)
		{
			Type = type;
			FilePath = filePath;
			FileName = new FileInfo(FilePath).Name;
		}

		public string FileName { get; set; }

		public string FilePath { get; set; }

		public HomeViewModel.Type Type { get; set; }

		public string FriendlyName { get { return string.Format("{0} - {1}", Type, FileName); } }
	}
}
