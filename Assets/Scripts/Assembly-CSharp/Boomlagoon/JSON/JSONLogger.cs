using UnityEngine;

namespace Boomlagoon.JSON
{
	internal static class JSONLogger
	{
		public static void Log(string str)
		{
			Debug.Log(str);
		}

		public static void Error(string str)
		{
			Debug.LogError(str);
		}
	}
}
