using System;

namespace Blamite.IO.IoStore
{
	/// <summary>
	///     The exception that is thrown when an IoStore container cannot be understood.
	/// </summary>
	/// <remarks>
	///     The container format has no redundancy and no per-array terminators, so a single
	///     mis-sized record silently turns the rest of the file into plausible-looking garbage.
	///     Every structural check in this namespace throws this rather than continuing.
	/// </remarks>
	public class IoStoreException : Exception
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="IoStoreException" /> class.
		/// </summary>
		/// <param name="message">A description of the problem that was found.</param>
		public IoStoreException(string message)
			: base(message)
		{
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="IoStoreException" /> class.
		/// </summary>
		/// <param name="message">A description of the problem that was found.</param>
		/// <param name="innerException">The exception which caused this one.</param>
		public IoStoreException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
