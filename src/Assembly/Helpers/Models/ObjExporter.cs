using System;
using System.IO;
using System.Linq;
using Blamite.Blam;
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
		private IRenderModel _model;
		private ICacheFile _cache;

		/// <summary>
		///     Constructs a new ObjExporter.
		/// </summary>
		/// <param name="outPath">The path of the file to write to. It will be overwritten.</param>
		/// <param name="cache"></param>
		public ObjExporter(string outPath, ICacheFile cache)
		{
			_output = new StreamWriter(File.Open(outPath, FileMode.Create, FileAccess.Write));
			_cache = cache;
		}

		public void Dispose()
		{
			_output.Dispose();
		}

		public void BeginModel(IRenderModel model)
		{
			_model = model;
			_output.WriteLine("# ------------------------------------------------- #");
			_output.WriteLine("# --------- Model Extracted with Assembly --------- #");
			_output.WriteLine("# ------------------------------------------------- #");
		}

		public void EndModel(IRenderModel model)
		{
			_model = null;
		}

		public void BeginSubmeshVertices(IModelSubmesh submesh)
		{
		}

		public void EndSubmeshVertices(IModelSubmesh submesh)
		{
		}

		public void BeginSection(int sectionIndex, IModelSection section)
		{
		}

		public void EndSection(int sectionIndex, IModelSection section)
		{
		}

		public void BeginVertex()
		{
		}

		public void EndVertex()
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

		public void ProcessSubmeshIndices(IModelSubmesh submesh, ushort[] indices, int baseIndex, int sectionIndex)
		{
			_output.WriteLine("# baseIndex = {0}", baseIndex);

			if (_model == null)
				throw new InvalidOperationException("The IModel of the current section is null.");

			StringID? sectionStringId = null;
			foreach (var region in _model.Regions)
			{
				foreach (var perm in region.Permutations.Where(perm => perm.ModelSectionIndex == sectionIndex))
				{
					sectionStringId = perm.Name;
					break;
				}
				if (sectionStringId != null)
					break;
			}
			_output.WriteLine("g {0}",
				sectionStringId == null
					? string.Format("g section_index:{0}", sectionIndex)
					: _cache.StringIDs.GetString((StringID) sectionStringId));

			// Models use triangle strips
			var flipTriangle = true;
			for (var i = 2; i < indices.Length; i++)
			{
				flipTriangle = !flipTriangle;

				var v0 = indices[i - 2] + 1 + baseIndex;
				var v1 = indices[i - 1] + 1 + baseIndex;
				var v2 = indices[i] + 1 + baseIndex;
				if (v0 == v1 || v0 == v2 || v1 == v2)
					continue; // Throw the triangle out

				// TODO: We should probably check if the vertices actually have normals and texture coordinates on them
				_output.WriteLine("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}", v0, flipTriangle ? v2 : v1, flipTriangle ? v1 : v2);
			}
		}

		public void Close()
		{
			Dispose();
		}
	}
}