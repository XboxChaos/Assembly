using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Shell;

namespace Assembly.Backend
{
    public class JumpLists
    {
        public static void UpdateJumplists()
        {
            JumpList jump = new JumpList();
            JumpList.SetJumpList(Application.Current, jump);

            if (Settings.applicationRecents != null)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (i > Settings.applicationRecents.Count - 1)
                        break;

                    JumpTask task = new JumpTask();
                    string openType = "";
                    int iconIndex = 200;
                    switch (Settings.applicationRecents[i].FileType)
                    {
                        case Settings.RecentFileType.BLF:
                            openType = "blf";
                            iconIndex = 200;
                            break;
                        case Settings.RecentFileType.Cache:
                            openType = "map";
                            iconIndex = 201;
                            break;
                        case Settings.RecentFileType.MapInfo:
                            openType = "mapinfo";
                            iconIndex = 202;
                            break;
                    }

                    task.ApplicationPath = VariousFunctions.GetApplicationAssemblyLocation();
                    task.Arguments = string.Format("assembly://open \"{1}\"", openType, Settings.applicationRecents[i].FilePath);
                    task.WorkingDirectory = VariousFunctions.GetApplicationLocation();

                    task.IconResourcePath = VariousFunctions.GetApplicationLocation() + "AssemblyIconLibrary.dll";
                    task.IconResourceIndex = iconIndex;

                    task.CustomCategory = "Recent";
                    task.Title = Settings.applicationRecents[i].FileName + " - " + Settings.applicationRecents[i].FileGame;
                    task.Description = string.Format("Open {0} in Assembly. ({1})", Settings.applicationRecents[i].FileName, Settings.applicationRecents[i].FilePath);

                    jump.JumpItems.Add(task);
                }
            }

            // Set events
            jump.JumpItemsRemovedByUser += jump_JumpItemsRemovedByUser;

            // Show Recent and Frequent categories :D
            jump.ShowFrequentCategory = false;
            jump.ShowRecentCategory = false;

            // Send to the Windows Shell
            jump.Apply();
        }

        private static void jump_JumpItemsRemovedByUser(object sender, JumpItemsRemovedEventArgs e)
        {
            IList<JumpItem> tasks = e.RemovedItems;

            foreach (JumpTask task in (IList<JumpTask>)e.RemovedItems)
            {

            }
        }
    }
}
