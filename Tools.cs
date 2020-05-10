using Sandbox.Definitions;
using VRage.Utils;

namespace WeaponsOverhaul
{
	public class Tools
	{
		public const float Tick = 1f / 60f;
		public const float MillisecondPerFrame = 1000f / 60f;
		public const double FireRateMultiplayer = 1d / 60d / 60d;
		public static float MaxSpeedLimit => ((MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed > MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed) ?
MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed : MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed) + 10;

		private const bool LogDebugMessages = true;
		private const string Prefix = "[WeaponsOverhaul] ";

		public static void Info(string message) 
		{
			MyLog.Default.Info($"{Prefix}{message}");
		}

		public static void Error(string message) 
		{
			MyLog.Default.Error($"{Prefix}{message}");
		}

		public static void Warning(string message)
		{
			MyLog.Default.Warning($"{Prefix}{message}");
		}

		public static void Debug(string message) 
		{
			if (LogDebugMessages)
			{
				MyLog.Default.Info($"[DEBUG] {Prefix}{message}");
			}
		}

		public static float GetScalerInverse(float mult)
		{
			if (mult > 1)
			{
				mult = 1 / mult;
			}
			else
			{
				mult = 1 + (1 - mult);
			}
			return mult;
		}

	}
}
