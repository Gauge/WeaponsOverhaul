using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace WeaponsOverhaul
{
	public class TurretBase : WeaponBase, ITurret
	{

		public const float KeenRotationMultiplyer = 16f;

		private bool unknownIdleElevation = true;
		public float Azimuth;
		public float Elevation;

		private float MinElevationRadians;
		private float MaxElevationRadians = MathHelper.Pi * 2f;
		private float MinAzimuthRadians;
		private float MaxAzimuthRadians = MathHelper.Pi * 2f;

		private float idleAzimuth = 0;
		private float idleElevation = 0;

		private MyEntitySubpart modelBase1;
		private MyEntitySubpart modelBase2;
		private MyEntitySubpart modelBarrel;

		private MyEntity Target;

		public override void SystemRestart()
		{
			base.SystemRestart();

			MinElevationRadians = MathHelper.ToRadians(Tools.NormalizeAngle(MinElevationDegrees));
			MaxElevationRadians = MathHelper.ToRadians(Tools.NormalizeAngle(MaxElevationDegrees));
			if (MinElevationRadians > MaxElevationRadians)
			{
				MinElevationRadians -= MathHelper.Pi * 2f;
			}
			MinAzimuthRadians = MathHelper.ToRadians(Tools.NormalizeAngle(MinAzimuthDegrees));
			MaxAzimuthRadians = MathHelper.ToRadians(Tools. NormalizeAngle(MaxAzimuthDegrees));
			if (MinAzimuthRadians > MaxAzimuthRadians)
			{
				MinAzimuthRadians -= MathHelper.Pi * 2f;
			}
		}

		public override void Update()
		{

			base.Update();
		}

		public override void Animate()
		{
			if (AiEnabled)
			{
				TargetAcquisition();
				if (Target != null)
				{
					//Target.PositionComp.WorldAABB.Center
					LookAt(Target.WorldMatrix.Translation);
				}

				RotateModels();

				MyAPIGateway.Utilities.ShowNotification($"Azimuth: {Azimuth} Elevation: {Elevation}", 1);
			}

			base.Animate();
		}

		private void LookAt(Vector3D target)
		{
			
			float az = 0;
			float elev = 0;

			Vector3D muzzleWorldPosition = gun.GunBase.GetMuzzleWorldPosition();
			Vector3.GetAzimuthAndElevation(Vector3.Normalize(Vector3D.TransformNormal(target - muzzleWorldPosition, CubeBlock.PositionComp.WorldMatrixInvScaled)), out az, out elev);
			if (unknownIdleElevation)
			{
				Vector3.GetAzimuthAndElevation(gun.GunBase.GetMuzzleLocalMatrix().Forward, out idleAzimuth, out idleElevation);
				unknownIdleElevation = false;
			}

			float targetElevation = elev - idleElevation;
			float targetAzimuth = az - idleAzimuth;
			float elevationSpeed = ElevationSpeed * KeenRotationMultiplyer;
			float azimuthSpeed = RotationSpeed * KeenRotationMultiplyer;

			float elevationTravel = MathHelper.Clamp(targetElevation - Elevation, -elevationSpeed, elevationSpeed);
			float azimuthTravel = MathHelper.Clamp(targetAzimuth - Azimuth, -azimuthSpeed, azimuthSpeed);

			float azimuthAngleDifference = targetAzimuth - Azimuth;

			MyAPIGateway.Utilities.ShowNotification($"azimuthAngleDifference: {azimuthAngleDifference}", 1);


			if (Math.Abs(azimuthAngleDifference) < Math.PI)
				Azimuth += azimuthTravel;
			else
				Azimuth -= azimuthTravel;

			Elevation = elevationTravel;

			//if (Azimuth < targetAzimuth)
			//{
			//	if (Math.Abs(azimuthAngleDifference) < 180)
			//		Azimuth += azimuthTravel;
			//	else
			//		Azimuth -= azimuthTravel;
			//}
			//else
			//{
			//	if (Math.Abs(azimuthAngleDifference) < 180)
			//		Azimuth -= azimuthTravel;
			//	else
			//		Azimuth += azimuthTravel;
			//}


			Azimuth = MathHelper.WrapAngle(Azimuth);

			//Elevation = elev - idleElevation;
			//Azimuth = MathHelper.WrapAngle(az - idleAzimuth);


		}

		private void TargetAcquisition()
		{
			try
			{
				List<MyEntity> targets = Block.CubeGrid.Components.Get<MyGridTargeting>().TargetRoots;

				Target = targets[0];

				//MyAPIGateway.Utilities.ShowNotification($"Target Count: {targets.Count}", 1);

			}
			catch
			{ }
		}

		protected void RotateModels()
		{
			if (modelBase1 == null || modelBase2 == null || modelBarrel == null || !modelBase1.Render.IsChild(0))
			{
				try
				{
					modelBase1 = CubeBlock.Subparts["GatlingTurretBase1"];
					modelBase2 = modelBase1.Subparts["GatlingTurretBase2"];
					modelBarrel = modelBase2.Subparts["GatlingBarrel"];
				}
				catch { }

				if (modelBase1 == null || modelBase2 == null || modelBarrel == null || !modelBase1.Render.IsChild(0))
				{
					return;
				}
			}

			ClampAzimuthAndElevation();

			Matrix azimuthRotation;
			Matrix.CreateRotationY(Azimuth, out azimuthRotation);
			Matrix localMatrixRef = modelBase1.PositionComp.LocalMatrixRef;
			azimuthRotation.Translation = localMatrixRef.Translation;
			Matrix blockLocalMatrix = CubeBlock.PositionComp.LocalMatrixRef;
			Matrix result2;
			Matrix.Multiply(ref azimuthRotation, ref blockLocalMatrix, out result2);
			modelBase1.PositionComp.SetLocalMatrix(ref azimuthRotation, modelBase1.Physics, false, ref result2, true);

			Matrix elevationRotation;
			Matrix.CreateRotationX(Elevation, out elevationRotation);
			localMatrixRef = modelBase2.PositionComp.LocalMatrixRef;
			elevationRotation.Translation = localMatrixRef.Translation;
			Matrix result4;
			Matrix.Multiply(ref elevationRotation, ref result2, out result4);
			modelBase2.PositionComp.SetLocalMatrix(ref elevationRotation, modelBase2.Physics, true, ref result4, true);
			gun.GunBase.WorldMatrix = modelBarrel.WorldMatrix;
		}

		private void ClampAzimuthAndElevation()
		{
			Azimuth = ClampAzimuth(Azimuth);
			Elevation = ClampElevation(Elevation);
		}

		private float ClampAzimuth(float value)
		{
			if (IsAzimuthLimited())
			{
				value = Math.Min(MaxAzimuthRadians, Math.Max(MinAzimuthRadians, value));
			}
			return value;
		}

		private float ClampElevation(float value)
		{
			if (IsElevationLimited())
			{
				value = Math.Min(MaxElevationRadians, Math.Max(MinElevationRadians, value));
			}
			return value;
		}

		private bool IsAzimuthLimited()
		{
			return Math.Abs(MaxAzimuthRadians - MinAzimuthRadians - MathHelper.Pi * 2f) > 0.01f;
		}

		private bool IsElevationLimited()
		{
			return Math.Abs(MaxElevationRadians - MinElevationRadians - MathHelper.Pi * 2f) > 0.01f;
		}
	}
}
