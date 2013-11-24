using Blamite.Flexibility;

namespace Blamite.Blam.Resources.Models
{
	/// <summary>
	///     A bounding box for a model.
	/// </summary>
	public class BoundingBox
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="BoundingBox" /> class.
		/// </summary>
		public BoundingBox()
		{
			Unknown1 = 3;
		}

		/// <summary>
		///     Gets or sets the minimum X coordinate for vertex positions in the model.
		/// </summary>
		public float MinX { get; set; }

		/// <summary>
		///     Gets or sets the maximum X coordinate for vertex positions in the model.
		/// </summary>
		public float MaxX { get; set; }

		/// <summary>
		///     Gets or sets the minimum Y coordinate for vertex positions in the model.
		/// </summary>
		public float MinY { get; set; }

		/// <summary>
		///     Gets or sets the maximum Y coordinate for vertex positions in the model.
		/// </summary>
		public float MaxY { get; set; }

		/// <summary>
		///     Gets or sets the minimum Z coordinate for vertex positions in the model.
		/// </summary>
		public float MinZ { get; set; }

		/// <summary>
		///     Gets or sets the maximum Z coordinate for vertex positions in the model.
		/// </summary>
		public float MaxZ { get; set; }

		/// <summary>
		///     Gets or sets the minimum U coordinate for texture coordinates in the model.
		/// </summary>
		public float MinU { get; set; }

		/// <summary>
		///     Gets or sets the maximum U coordinate for texture coordinates in the model.
		/// </summary>
		public float MaxU { get; set; }

		/// <summary>
		///     Gets or sets the minimum V coordinate for texture coordinates in the model.
		/// </summary>
		public float MinV { get; set; }

		/// <summary>
		///     Gets or sets the maximum V coordinate for texture coordinates in the model.
		/// </summary>
		public float MaxV { get; set; }

		/// <summary>
		///     Usually 3
		/// </summary>
		public int Unknown1 { get; set; }

		/// <summary>
		///     Zero
		/// </summary>
		public int Unknown2 { get; set; }

		/// <summary>
		///     Zero
		/// </summary>
		public int Unknown3 { get; set; }

		/// <summary>
		///     Zero
		/// </summary>
		public int Unknown4 { get; set; }

		/// <summary>
		///     Deserializes a bounding box from a set of values read from a structure.
		/// </summary>
		/// <param name="values">The values to use.</param>
		/// <returns>The bounding box.</returns>
		public static BoundingBox Deserialize(StructureValueCollection values)
		{
			var result = new BoundingBox();
			result.MinX = values.GetFloat("min x");
			result.MaxX = values.GetFloat("max x");
			result.MinY = values.GetFloat("min y");
			result.MaxY = values.GetFloat("max y");
			result.MinZ = values.GetFloat("min z");
			result.MaxZ = values.GetFloat("max z");
			result.MinU = values.GetFloat("min u");
			result.MaxU = values.GetFloat("max u");
			result.MinV = values.GetFloat("min v");
			result.MaxV = values.GetFloat("max v");
			result.Unknown1 = (int) values.GetIntegerOrDefault("unknown 1", 3);
			result.Unknown2 = (int) values.GetIntegerOrDefault("unknown 2", 0);
			result.Unknown3 = (int) values.GetIntegerOrDefault("unknown 3", 0);
			result.Unknown4 = (int) values.GetIntegerOrDefault("unknown 4", 0);
			return result;
		}

		/// <summary>
		///     Serializes this instance.
		/// </summary>
		/// <returns>A writable set of structure values.</returns>
		public StructureValueCollection Serialize()
		{
			var result = new StructureValueCollection();
			result.SetFloat("min x", MinX);
			result.SetFloat("max x", MaxX);
			result.SetFloat("min y", MinY);
			result.SetFloat("max y", MaxY);
			result.SetFloat("min z", MinZ);
			result.SetFloat("max z", MaxZ);
			result.SetFloat("min u", MinU);
			result.SetFloat("max u", MaxU);
			result.SetFloat("min v", MinV);
			result.SetFloat("max v", MaxV);
			result.SetInteger("unknown 1", (uint) Unknown1);
			result.SetInteger("unknown 2", (uint) Unknown2);
			result.SetInteger("unknown 3", (uint) Unknown3);
			result.SetInteger("unknown 4", (uint) Unknown4);
			return result;
		}
	}
}