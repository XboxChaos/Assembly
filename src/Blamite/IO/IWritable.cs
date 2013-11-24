namespace Blamite.IO
{
	public interface IWritable
	{
		/// <summary>
		///     Serializes the object, writing it to an IWriter.
		/// </summary>
		/// <param name="writer">The IWriter to write to.</param>
		void WriteTo(IWriter writer);
	}
}