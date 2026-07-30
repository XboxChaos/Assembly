using System;
using Blamite.Util;

namespace Blamite.Blam.FifthGen.Structures
{
	/// <summary>
	///     A Campaign Evolved UE package identity: the 64-bit value an IoStore <c>FIoChunkId</c>'s
	///     <c>PackageId</c> field carries, and what pairs a tag's <c>.uasset</c>/<c>.ubulk</c> chunks
	///     together (see <see cref="Blamite.IO.IoStore.IoChunkId" />).
	/// </summary>
	/// <remarks>
	///     A container hands this value over directly - reading a tag never needs to compute one.
	///     <see cref="FromName" /> exists for the opposite direction: turning a known or guessed
	///     package path into the ID it should hash to, to check a naming guess against reality. See
	///     <see cref="CityHash64" /> for the verification this was checked against.
	/// </remarks>
	public struct FifthGenPackageId : IEquatable<FifthGenPackageId>
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="FifthGenPackageId" /> struct.
		/// </summary>
		/// <param name="value">The package's 64-bit ID.</param>
		public FifthGenPackageId(ulong value)
		{
			Value = value;
		}

		/// <summary>
		///     Gets the package's 64-bit ID.
		/// </summary>
		public ulong Value { get; private set; }

		/// <summary>
		///     Computes the package ID a full UE package name should hash to.
		/// </summary>
		/// <param name="packageName">
		///     The package's full name, e.g.
		///     <c>/Game/Tags/objects/characters/spartans/spartans-biped</c>.
		/// </param>
		/// <returns>The package ID <paramref name="packageName" /> hashes to.</returns>
		public static FifthGenPackageId FromName(string packageName)
		{
			return new FifthGenPackageId(CityHash64.Compute(packageName));
		}

		public bool Equals(FifthGenPackageId other)
		{
			return Value == other.Value;
		}

		public override bool Equals(object obj)
		{
			return (obj is FifthGenPackageId) && Equals((FifthGenPackageId) obj);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		public override string ToString()
		{
			return string.Format("0x{0:X16}", Value);
		}

		public static bool operator ==(FifthGenPackageId a, FifthGenPackageId b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(FifthGenPackageId a, FifthGenPackageId b)
		{
			return !a.Equals(b);
		}
	}
}
