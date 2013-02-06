using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace VoxelSheetConverter
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		private void btnOpenVoxel_Click(object sender, EventArgs e)
		{
			var ofd = new OpenFileDialog
				          {
					          Title = "Open a Vorxel XML Definitions Sheet",
					          Filter = "XML Files(*.xml)|*.xml"
				          };
			if (ofd.ShowDialog() == DialogResult.OK)
				txtVoxelXmlDefinition.Text = ofd.FileName;
		}

		public class VoxelDataContainer
		{
			public VoxelData Voxel_Data { get; set; }

			public class VoxelData
			{
				public IList<PositionData> Position { get; set; }

				public class PositionData
				{
					public string Object { get; set; }
					public string Material { get; set; }
					public float x { get; set; }
					public float y { get; set; }
					public float z { get; set; }
				}
			}
		}
		private void btnConvert_Click(object sender, EventArgs e)
		{
			var doc = new XmlDocument();
			doc.LoadXml(File.ReadAllText(txtVoxelXmlDefinition.Text));
			var jsonText = JsonConvert.SerializeXmlNode(doc);
			var output = JsonConvert.DeserializeObject<VoxelDataContainer>(jsonText);

			txtOutputFloats.Text = "";
			foreach(var voxel in output.Voxel_Data.Position)
			{
				txtOutputFloats.Text += string.Format("{0}{1}{2} - ",
					FloatToHexString(voxel.x),
					FloatToHexString(voxel.y), 
					FloatToHexString(voxel.z));
			}
		}

		public string FloatToHexString(float val)
		{
			var floatBytes = BitConverter.GetBytes(val);
			Array.Reverse(floatBytes);
			var output = "";
			foreach (var floatByte in floatBytes)
			{
				if (floatByte == 0x00)
					output += "00";
				else
					output = output + floatByte.ToString("X");
			}

			return output;
		}
	}
}
