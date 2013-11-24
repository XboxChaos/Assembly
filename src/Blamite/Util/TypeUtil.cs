using System;

namespace Blamite.Util
{
	/// <summary>
	///     Provides utility functions for working with .NET types.
	/// </summary>
	public static class TypeUtil
	{
		/// <summary>
		///     Determines whether or not a type is an integer.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool IsInteger(Type type)
		{
			if (type == null)
				return false;

			// Handle the case where the type could be generic
			if (IsInteger(Nullable.GetUnderlyingType(type)))
				return true;

			// Just check the type code
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.SByte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
					return true;
			}
			return false;
		}
	}
}