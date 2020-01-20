using System;
using System.Collections.Generic;

namespace Assembly.Helpers
{
	public class ExceptionDictionary
	{
		public static Exception GetFriendlyException(Exception ex)
		{
			var exceptions = new Dictionary<Type, Exception>
			{
				{ typeof(UnauthorizedAccessException), new Exception("An UnauthorizedAccessException has occurred.\r\n\r\n"
						+ "Chances are the map/content file you just tried to modify is set to readonly.\r\n"
						+ "Verify the readonly status of your file and try again before reporting this.\r\n\r\n", ex)},

			};

			Exception result;
			if (exceptions.TryGetValue(ex.GetType(), out result))
				return result;
			else
				return ex;
		}
	}
}