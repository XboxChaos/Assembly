using System.Windows;
using System.Windows.Shell;

namespace Assembly.Helpers
{
    public class JumpLists
    {
        public static void UpdateJumplists()
        {
			var jump = new JumpList();
            JumpList.SetJumpList(Application.Current, jump);

            if (Settings.applicationRecents != null)
            {
                for (var i = 0; i < 10; i++)
                {
                    if (i > Settings.applicationRecents.Count - 1)
                        break;

                    var task = new JumpTask();
					var iconIndex = -200;
                    switch (Settings.applicationRecents[i].FileType)
                    {
                        case Settings.RecentFileType.Blf:
                            iconIndex = -200;
                            break;
                        case Settings.RecentFileType.Cache:
                            iconIndex = -201;
                            break;
                        case Settings.RecentFileType.MapInfo:
                            iconIndex = -202;
                            break;
                    }

                    task.ApplicationPath = VariousFunctions.GetApplicationAssemblyLocation();
                    task.Arguments = string.Format("assembly://open \"{0}\"", Settings.applicationRecents[i].FilePath);
                    task.WorkingDirectory = VariousFunctions.GetApplicationLocation();

                    task.IconResourcePath = VariousFunctions.GetApplicationLocation() + "AssemblyIconLibrary.dll";
                    task.IconResourceIndex = iconIndex;

                    task.CustomCategory = "Recent";
                    task.Title = Settings.applicationRecents[i].FileName + " - " + Settings.applicationRecents[i].FileGame;
                    task.Description = string.Format("Open {0} in Assembly. ({1})", Settings.applicationRecents[i].FileName, Settings.applicationRecents[i].FilePath);

                    jump.JumpItems.Add(task);
                }
            }

            // Show Recent and Frequent categories :D
            jump.ShowFrequentCategory = false;
            jump.ShowRecentCategory = false;

            // Send to the Windows Shell
            jump.Apply();
        }
    }
}
