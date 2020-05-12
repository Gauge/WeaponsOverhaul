using Sandbox.Definitions;
using System;
using VRage.Utils;
using VRageMath;

namespace WeaponsOverhaul
{
	public class Tools
	{
		public const float Tick = 1f / 60f;
		public const float MillisecondPerFrame = 1000f / 60f;
		public const double FireRateMultiplayer = 1d / 60d / 60d;

		public static Random Random { get; } = new Random(77658);

		public static float MaxSpeedLimit => ((MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed > MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed) ?
MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed : MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed) + 10;

		public static bool DebugMode = false;
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
			if (DebugMode)
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

		private const int Seed = 5366354;
		private static float[] RandomSet;
		private static float[] RandomSetFromAngle;
		public static Vector3 ApplyDeviation(Vector3 direction, float maxAngle, ref sbyte index) 
		{
			if (maxAngle == 0)
				return direction;

			if (RandomSet == null)
			{
				RandomSet = new float[128];
				RandomSetFromAngle = new float[128];

				Random rand = new Random(Seed);

				for (int i = 0; i < 128; i++)
				{
					RandomSet[i] = (float)(rand.NextDouble() * Math.PI * 2);
				}

				for (int i = 0; i < 128; i++)
				{
					RandomSetFromAngle[i] = (float)rand.NextDouble();
				}
			}

			if (index == 127)
			{
				index = 0;
			}
			else
			{
				index++;
			}

			Matrix matrix = Matrix.CreateFromDir(direction);

			float randomFloat = (RandomSetFromAngle[index] * maxAngle * 2) - maxAngle;
			float randomFloat2 = RandomSet[index];

			Vector3 normal = -new Vector3(MyMath.FastSin(randomFloat) * MyMath.FastCos(randomFloat2), MyMath.FastSin(randomFloat) * MyMath.FastSin(randomFloat2), MyMath.FastCos(randomFloat));
			return Vector3.TransformNormal(normal, matrix);
		}




	}
}
