using Newtonsoft.Json;
using System;

namespace DiscordBot.Common.Utils
{
	public static class Extensions
	{
		public static string ToJsonString(this object that, bool indent = true) => JsonConvert.SerializeObject(that, indent? Formatting.Indented:Formatting.None);
		public static bool IsNullOrEmpty(this string that) => string.IsNullOrEmpty(that);
		public static void ActIfNull(this object that, Action action)
		{
			if (that == null) action();
		}

		public static void ActIfNotNull(this object that, Action action)
		{
			if (that != null) action();
		}
		public static bool IsInt(this string that, out int result)
		{
			if (that.IsNullOrEmpty())
			{
				result = 0;
				return false;
			}
			return int.TryParse(that, out result);
		}
	}


}
