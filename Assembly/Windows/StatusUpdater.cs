using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using Assembly.Helpers;

namespace Assembly.Windows
{
    public class StatusUpdater
    {
        /// <summary>
        /// Update the status of the application. 
        /// </summary>
        /// <param name="update">The new status</param>
        public static void Update(string update)
        {
            Settings.homeWindow.UpdateStatusText(update);
        }
    }
}