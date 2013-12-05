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

		public class WaveFrontObject
		{
			public IList<Face> FaceDefinitions { get; set; }

			public int BaseIndex { get; set; }

			public string Name { get; set; }

			public class Vertex
			{
				public float X { get; set; }
				public float Y { get; set; }
				public float Z { get; set; }
			}

			public class Face
			{
				public IList<Element> Elements { get; set; } 

				public class Element
				{
					public Vertex Vertex { get; set; }
					public Vertex VertexTextureCoordinate { get; set; }
					public Vertex VertexNormal { get; set; }
				}
			}
		}

		public void Process()
		{
			var wavefrontObject = new WaveFrontObject
			{
				FaceDefinitions = new List<WaveFrontObject.Face>(),
				Name = "",
				BaseIndex = 0
			};

			var vertices = new List<WaveFrontObject.Vertex>();
			var vertexNormals = new List<WaveFrontObject.Vertex>();
			var vertexTextureCoordinates = new List<WaveFrontObject.Vertex>();

			while (!_input.EndOfStream)
			{
				var line = _input.ReadLine();
				if (line == null)
					break;
				line = line.ToLower();

				if (line.StartsWith("v"))
				{
					var coords = line.Remove(0, 2).Trim().Split(' ');
					var vertex = new WaveFrontObject.Vertex
					{
						X = float.Parse(coords[0]),
						Y = float.Parse(coords[1]),
						Z = float.Parse(coords[2]),
					};

					if (line.StartsWith("vn"))
						vertices.Add(vertex);
					else if (line.StartsWith("vt"))
						vertices.Add(vertex);
					else if (line.StartsWith("v"))
						vertices.Add(vertex);
				}
				else if (line.StartsWith("# baseindex = "))
					wavefrontObject.BaseIndex = int.Parse(line.Replace("# baseindex = ", ""));
				else if (line.StartsWith("g "))
					wavefrontObject.Name = line.Replace("g", "").Trim();
				else if (line.StartsWith("f "))
				{
					var match = Regex.Match(line, @"f[ ]+([0-9\/]+) ([0-9\/]+) ([0-9\/]+)", RegexOptions.IgnoreCase);
					var face = new WaveFrontObject.Face { Elements = new List<WaveFrontObject.Face.Element>() };
					for (var i = 1; i < 4; i++)
					{
						var section = match.Groups[i].Captures[0].Value.Split('/');
						var vertexIndex = int.Parse(section[0]) - 1;
						var vertexTextureCoordinateIndex = int.Parse(section[1]) - 1;
						var vertexNormalIndex = int.Parse(section[2]) - 1;

						face.Elements.Add(new WaveFrontObject.Face.Element
						{
							Vertex = vertices[vertexIndex],
							VertexTextureCoordinate = vertexTextureCoordinates[vertexTextureCoordinateIndex],
							VertexNormal = vertexNormals[vertexNormalIndex],
						});
					}
					wavefrontObject.FaceDefinitions.Add(face);
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
