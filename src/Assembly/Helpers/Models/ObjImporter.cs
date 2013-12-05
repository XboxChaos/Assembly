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
			public IList<Vertex> Vertices { get; set; }
			public IList<Vertex> VertexTextureCoordinates { get; set; }
			public IList<Vertex> VertexNormals { get; set; }
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
					public int VertexIndex { get; set; }
					public int VertexTextureCoordinateIndex { get; set; }
					public int VertexNormalIndex { get; set; }
				}
			}
		}

		public WaveFrontObject Process()
		{
			var wavefrontObject = new WaveFrontObject
			{
				FaceDefinitions = new List<WaveFrontObject.Face>(),
				Vertices = new List<WaveFrontObject.Vertex>(),
				VertexTextureCoordinates = new List<WaveFrontObject.Vertex>(),
				VertexNormals = new List<WaveFrontObject.Vertex>(),
				Name = "",
				BaseIndex = 0
			};

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
						wavefrontObject.VertexNormals.Add(vertex);
					else if (line.StartsWith("vt"))
						wavefrontObject.VertexTextureCoordinates.Add(vertex);
					else if (line.StartsWith("v"))
						wavefrontObject.Vertices.Add(vertex);
				}
				else if (line.StartsWith("# baseindex = "))
					wavefrontObject.BaseIndex = int.Parse(line.Replace("# baseindex = ", ""));
				else if (line.StartsWith("g "))
					wavefrontObject.Name = line.Replace("g", "").Trim();
				else if (line.StartsWith("f "))
				{
					var match = Regex.Match(line, @"f[ ]+([0-9\/]+) ([0-9\/]+) ([0-9\/]+)", RegexOptions.IgnoreCase);
					var face = new WaveFrontObject.Face { Elements = new List<WaveFrontObject.Face.Element>() };
					for (var i = 1; i < match.Groups.Count; i++)
					{
						var section = match.Groups[i].Captures[0].Value.Split('/');
						var vertexIndex = int.Parse(section[0]) - 1;
						var vertexTextureCoordinateIndex = int.Parse(section[1]) - 1;
						var vertexNormalIndex = int.Parse(section[2]) - 1;

						face.Elements.Add(new WaveFrontObject.Face.Element
						{
							VertexIndex = vertexIndex,
							VertexTextureCoordinateIndex = vertexTextureCoordinateIndex,
							VertexNormalIndex = vertexNormalIndex,
						});
					}
					wavefrontObject.FaceDefinitions.Add(face);
				}

				Debug.WriteLine(line);
			}

			return wavefrontObject;
		}

		public void Dispose()
		{
			_input.Dispose();
		}
	}
}
