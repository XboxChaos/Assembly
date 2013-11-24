using System;
using System.IO;
using Blamite.Blam.Resources.Models;
using Blamite.Flexibility;

namespace Assembly.Helpers.Models
{
	/// <summary>
	///     An IModelProcessor which exports model data to a Wavefront .obj file.
	/// </summary>
	public class ObjExporter : IModelProcessor, IDisposable
	{
		private readonly StreamWriter _output;

		/// <summary>
		///     Constructs a new ObjExporter.
		/// </summary>
		/// <param name="outPath">The path of the file to write to. It will be overwritten.</param>
		public ObjExporter(string outPath)
		{
			_output = new StreamWriter(File.Open(outPath, FileMode.Create, FileAccess.Write));
		}

		public void Dispose()
		{
			_output.Dispose();
		}

		public void BeginModel(IModel model)
		{
		}

		public void EndModel(IModel model)
		{
		}

		public void BeginSubmeshVertices(IModelSubmesh submesh)
		{
		}

		public void EndSubmeshVertices(IModelSubmesh submesh)
		{
		}

		public void BeginVertex()
		{
		}

		public void ProcessVertexElement(float x, float y, float z, float w, VertexElementLayout layout)
		{
			switch (layout.Usage)
			{
				case VertexElementUsage.Position:
					_output.WriteLine("v {0} {1} {2}", x, y, z);
					break;

				case VertexElementUsage.Normal:
					_output.WriteLine("vn {0} {1} {2}", x, y, z);
					break;

				case VertexElementUsage.TexCoords:
					_output.WriteLine("vt {0} {1} {2}", x, y, z);
					break;
			}
		}

		public void EndVertex()
		{
		}

		public void ProcessSubmeshIndices(IModelSubmesh submesh, ushort[] indices, int baseIndex)
		{
			_output.WriteLine("# baseIndex = {0}", baseIndex);

			// Models use triangle strips
			// TODO: Do we need to account for back-face culling here?
			for (int i = 2; i < indices.Length; i++)
			{
				int v0 = indices[i - 2] + 1 + baseIndex;
				int v1 = indices[i - 1] + 1 + baseIndex;
				int v2 = indices[i] + 1 + baseIndex;
				if (v0 == v1 || v0 == v2 || v1 == v2)
					continue; // Throw the triangle out

				// TODO: We should probably check if the vertices actually have normals and texture coordinates on them
				_output.WriteLine("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}", v0, v1, v2);
			}
		}

		public void Close()
		{
			Dispose();
		}
	}
}