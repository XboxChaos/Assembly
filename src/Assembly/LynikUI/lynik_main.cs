using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Assembly.Metro.Controls.PageTemplates.Games;
using Assembly.Helpers;
using Assembly.Helpers.Native;
using Assembly.Helpers.Net;
using Assembly.Metro.Controls.PageTemplates;
using Assembly.Metro.Controls.PageTemplates.Tools;
using Assembly.Metro.Controls.PageTemplates.Tools.Halo4;
using Assembly.Metro.Dialogs;
using System.IO;




namespace Assembly.LynikUI
{
    public partial class lynik_main : Form
    {
        private Windows.Home _classCalledFrom;
        private HaloMap _haloMap;
        public lynik_main()
        {
            InitializeComponent();
        }

        public void initializeValues(Windows.Home parentClass)
        {
            _classCalledFrom = parentClass;
                       
        }

		private delegate void ContentFileHandler(Windows.Home home, string path);

		private readonly Dictionary<string, ContentFileHandler> _contentFileHandlers = new Dictionary
	<string, ContentFileHandler>
		{
			{ ".map", (home, path) => home.AddCacheTabModule(path) },
			{ ".blf", (home, path) => home.AddImageTabModule(path) },
			{ ".mapinfo", (home, path) => home.AddInfooTabModule(path) },
			{ ".campaign", (home, path) => home.AddCampaignTabModule(path) },
			{ ".asmp", (home, path) => home.AddPatchTabModule(path) },
			{ ".ascpatch", (home, path) => home.AddPatchTabModule(path) },
			{ ".patchdat", (home, path) => home.AddPatchTabModule(path) },
			{ ".module", (home, path) => home.AddCacheTabModule(path) }
		};

		/// <summary>
		///     Open a new Blam Engine File
		/// </summary>
		public void OpenContentFile()
		{
			var filter = "Blam Files|" + _contentFileHandlers.Keys.Select(e => "*" + e).Aggregate((f, n) => f + ";" + n);
			var ofd = new OpenFileDialog
			{
				Title = "Assembly - Open File",
				Multiselect = true,
				Filter = filter
			};

			ofd.ShowDialog();

			foreach (string file in ofd.FileNames)
				ProcessContentFile(file);
		}

		public void ProcessContentFile(string file)
		{
			if (!File.Exists(file))
			{
				MetroMessageBox.Show("Unable to find file", "The selected file could no longer be found");
				return;
			}

			var extension = (Path.GetExtension(file) ?? "").ToLowerInvariant();
			ContentFileHandler handler;
			if (!_contentFileHandlers.TryGetValue(extension, out handler))
			{
				MetroMessageBox.Show("Assembly - Unsupported File Type",
					"\"" + file + "\" cannot be opened because its extension is not recognized.");
				return;
			}
			handler(_classCalledFrom, file);
		}

        private void btnDoTheThings_Click(object sender, EventArgs e)
        {
			OpenContentFile();

		}
    }
}
