using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Blamite.Blam;
using Blamite.IO;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.Editors
{
	/// <summary>
	/// Interaction logic for BspEditor.xaml
	/// </summary>
	public partial class BspEditor : Page
	{
		private readonly TagEntry _tag;
        private readonly ICacheFile _cache;
	    private readonly IStreamManager _streamManager;

		public BspEditor(TagEntry tag, ICacheFile cache, IStreamManager streamManager)
		{
			InitializeComponent();

			_tag = tag;
			_cache = cache;
			_streamManager = streamManager;
		}
	}
}
