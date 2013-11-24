using System;
using Blamite.Flexibility;

namespace Blamite.Blam.Resources.Models
{
	/// <summary>
	///     Calculates a model's bounding box.
	/// </summary>
	public class BoundingBoxCalculator
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="BoundingBoxCalculator" /> class.
		/// </summary>
		public BoundingBoxCalculator()
		{
			CalculatedBox = new BoundingBox();

			CalculatedBox.MinX = float.MaxValue;
			CalculatedBox.MinY = float.MaxValue;
			CalculatedBox.MinZ = float.MaxValue;
			CalculatedBox.MinU = float.MaxValue;
			CalculatedBox.MinV = float.MaxValue;

			CalculatedBox.MaxX = float.MinValue;
			CalculatedBox.MaxY = float.MinValue;
			CalculatedBox.MaxZ = float.MinValue;
			CalculatedBox.MaxU = float.MinValue;
			CalculatedBox.MaxV = float.MinValue;
		}

		/// <summary>
		///     Gets the calculated bounding box.
		/// </summary>
		public BoundingBox CalculatedBox { get; private set; }

		/// <summary>
		///     Processes a vertex, updating the current bounding box if it lies outside of it.
		/// </summary>
		/// <param name="x">The X component of the vertex.</param>
		/// <param name="y">The Y component of the vertex.</param>
		/// <param name="z">The Z component of the vertex.</param>
		/// <param name="usage">The vertex's usage.</param>
		public void ProcessVertex(float x, float y, float z, VertexElementUsage usage)
		{
			switch (usage)
			{
				case VertexElementUsage.Position:
					CalculatedBox.MinX = Math.Min(CalculatedBox.MinX, x);
					CalculatedBox.MinY = Math.Min(CalculatedBox.MinY, y);
					CalculatedBox.MinZ = Math.Min(CalculatedBox.MinZ, z);
					CalculatedBox.MaxX = Math.Max(CalculatedBox.MaxX, x);
					CalculatedBox.MaxY = Math.Max(CalculatedBox.MaxY, y);
					CalculatedBox.MaxZ = Math.Max(CalculatedBox.MaxZ, z);
					break;

				case VertexElementUsage.TexCoords:
					CalculatedBox.MinU = Math.Min(CalculatedBox.MinU, x);
					CalculatedBox.MinV = Math.Min(CalculatedBox.MinV, y);
					CalculatedBox.MaxU = Math.Max(CalculatedBox.MaxU, x);
					CalculatedBox.MaxV = Math.Max(CalculatedBox.MaxV, y);
					break;
			}
		}
	}
}