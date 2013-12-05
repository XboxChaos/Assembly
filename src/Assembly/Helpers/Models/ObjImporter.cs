using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Assembly.Helpers.Models
{
	public class ObjImporter : IDisposable
	{
		private readonly StreamReader _input;

		/// <summary>
		///     Constructs a new ObjImporter.
		/// </summary>
		/// <param name="inPath">The path of the file to load from.</param>
		public ObjImporter(string inPath)
		{
			_input = new StreamReader(File.Open(inPath, FileMode.Open, FileAccess.Read));
		}

		public class Temp
		{
			public List<ushort> Indices { get; set; }

			public int BaseIndex { get; set; }

			public string Name { get; set; }
		}

		public void Process()
		{
			var stuff = new Temp
			{
				Indices = new List<ushort>(),
				Name = "",
				BaseIndex = 0
			};

			while (!_input.EndOfStream)
			{
				var line = _input.ReadLine();
				if (line == null)
					break;
				line = line.ToLower();

				if (line.StartsWith("# baseIndex = "))
					stuff.BaseIndex = int.Parse(line.Replace("# baseIndex = ", ""));
				else if (line.StartsWith("g "))
					stuff.Name = line.Replace("g ", "");
				else if (line.StartsWith("f "))
				{
					var match = Regex.Match(line, @"f[ ]+([0-9\/])+ ([0-9\/])+ ([0-9\/])+", RegexOptions.IgnoreCase);
					for(var i = 1; i < 4; i++)
						stuff.Indices.Add(ushort.Parse(match.Groups[i].Captures[0].Value));
				}


				Debug.WriteLine(line);
			}
		}

		public void Dispose()
		{
			_input.Dispose();
		}
	}
}
