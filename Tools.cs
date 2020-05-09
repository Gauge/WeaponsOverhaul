using System;
using System.Collections.Generic;
using System.Text;
using VRage.Utils;

namespace WeaponsOverhaul
{
	public class Tools
	{

		private const bool LogDebugMessages = true;
		private const string prefix = "[WeaponsOverhaul] ";

		public static void Info(string message) 
		{
			MyLog.Default.Info($"{prefix}{message}");
		}

		public static void Error(string message) 
		{
			MyLog.Default.Error($"{prefix}{message}");
		}

		public static void Debug(string message) 
		{
			if (LogDebugMessages)
			{
				MyLog.Default.Debug($"{prefix}{message}");
			}
		}

	}
}
