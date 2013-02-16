/* Copyright 2012 Aaron Dierking, TJ Tunnell, Jordan Mueller, Alex Reed
 * 
 * This file is part of Extryze.
 * 
 * Extryze is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Extryze is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Extryze.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Xml.Linq;
using ExtryzeDLL.IO;
using ExtryzeDLL.Blam.ThirdGen;
using ExtryzeDLL.Blam.ThirdGen.Structures;
using ExtryzeDLL.Util;
using ExtryzeDLL.Blam;
using ExtryzeDLL.Flexibility;
using System.Reflection;

namespace Assembly
{
    public partial class Form1 : Form
    {
        private ThirdGenCacheFile _map = null;
        private IReader _reader = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Open Cache File";
            ofd.Filter = "Blam Cache Files|*.map";
            if (ofd.ShowDialog() == DialogResult.Cancel)
                return;

            txtMapPath.Text = ofd.FileName;

            Stream fileStream = File.OpenRead(ofd.FileName);
            _reader = new EndianReader(fileStream, Endian.BigEndian);

            lbStrings.Items.Clear();
            CacheFileVersionInfo version = new CacheFileVersionInfo(_reader);
            XDocument supportedBuilds = XDocument.Load(@"Formats\SupportedBuilds.xml");
            BuildInfoLoader layoutLoader = new BuildInfoLoader(supportedBuilds, @"Formats\");
            BuildInformation buildInfo = layoutLoader.LoadBuild(version.BuildString);
            if (buildInfo == null)
            {
                MessageBox.Show("Unsupported engine build \"" + version.BuildString + "\"", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            _map = new ThirdGenCacheFile(_reader, buildInfo, version.BuildString);

            txtGame.Text = buildInfo.GameName;
            txtBuild.Text = _map.BuildString.ToString();
            txtType.Text = _map.Type.ToString();
            txtInternalName.Text = _map.InternalName;
            txtScenarioName.Text = _map.ScenarioName;
            txtVirtBase.Text = "0x" + _map.MetaArea.BasePointer.ToString("X8");
            txtVirtSize.Text = "0x" + _map.MetaArea.Size.ToString("X");
            txtXdk.Text = _map.XDKVersion.ToString();
            txtRawTableOffset.Text = "0x" + _map.RawTable.Offset.ToString("X8");
            txtRawTableSize.Text = "0x" + _map.RawTable.Offset.ToString("X");
            txtIndexHeaderAddr.Text = "0x" + _map.IndexHeaderLocation.AsPointer().ToString("X8");
            txtIndexOffsetMagic.Text = "0x" + _map.LocaleArea.PointerMask.ToString("X8");
            txtMapMagic.Text = "0x" + _map.MetaArea.OffsetToPointer(0).ToString("X8");

            lbClasses.Items.Clear();
            tvTags.Nodes.Clear();
            Dictionary<ITagClass, TreeNode> classNodes = new Dictionary<ITagClass, TreeNode>();
            foreach (ITagClass tagClass in _map.TagClasses)
            {
                string className = CharConstant.ToString(tagClass.Magic);
                lbClasses.Items.Add(className);
                classNodes[tagClass] = tvTags.Nodes.Add(className);
            }

            for (int i = 0; i < _map.Tags.Count; i++)
            {
                ITag tag = _map.Tags[i];
                if (tag.Index.IsValid)
                {
                    string name = _map.FileNames.FindTagName(tag.Index);
                    if (name == null)
                        name = tag.Index.ToString();
                    classNodes[tag.Class].Nodes.Add(name);
                }
            }

            lbLanguages.Items.Clear();
            foreach (FieldInfo language in typeof(LocaleLanguage).GetFields())
                lbLanguages.Items.Add(language.Name);
        }

        private void lbLanguages_SelectedIndexChanged(object sender, EventArgs e)
        {
            lbStrings.Items.Clear();
            int language = lbLanguages.SelectedIndex;
            LocaleTable strings = _map.Languages[language].LoadStrings(_reader);
            foreach (Locale str in strings.Strings)
                lbStrings.Items.Add(str.Value);
        }
    }
}
