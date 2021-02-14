using Sandbox.Game.Entities;
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
		public enum TargetFlags : ushort
		{
			Meteors = 0x1,
			Missiles = 0x2,
			SmallShips = 0x4,
			LargeShips = 0x8,
			Characters = 0x10,
			Stations = 0x20,
			Neutrals = 0x40
		}

		public const float KeenRotationMultiplyer = 16f;

		private bool unknownIdleElevation = true;
		public float Azimuth;
		public float Elevation;

		private float AzimuthSpeedPerTick;
		private float ElevationSpeedPerTick;

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

		private TargetFlags TargetMode = TargetFlags.LargeShips & TargetFlags.SmallShips & TargetFlags.Characters & TargetFlags.Stations;

		public bool TargetMeteors
		{
			get 
			{
				return (TargetMode & TargetFlags.Meteors) != 0;
			}
			set 
			{
				if (value)
				{
					TargetMode |= TargetFlags.Meteors;
				}
				else
				{
					TargetMode &= ~TargetFlags.Meteors;
				}
			}
		}

		public bool TargetMissiles
		{
			get
			{
				return (TargetMode & TargetFlags.Missiles) != 0;
			}
			set
			{
				if (value)
				{
					TargetMode |= TargetFlags.Missiles;
				}
				else
				{
					TargetMode &= ~TargetFlags.Missiles;
				}
			}
		}

		public bool TargetSmallShips
		{
			get
			{
				return (TargetMode & TargetFlags.SmallShips) != 0;
			}
			set
			{
				if (value)
				{
					TargetMode |= TargetFlags.SmallShips;
				}
				else
				{
					TargetMode &= ~TargetFlags.SmallShips;
				}
			}
		}

		public bool TargetLargeShips
		{
			get
			{
				return (TargetMode & TargetFlags.LargeShips) != 0;
			}
			set
			{
				if (value)
				{
					TargetMode |= TargetFlags.LargeShips;
				}
				else
				{
					TargetMode &= ~TargetFlags.LargeShips;
				}
			}
		}

		public bool TargetCharacters
		{
			get
			{
				return (TargetMode & TargetFlags.Characters) != 0;
			}
			set
			{
				if (value)
				{
					TargetMode |= TargetFlags.Characters;
				}
				else
				{
					TargetMode &= ~TargetFlags.Characters;
				}
			}
		}

		public bool TargetStations
		{
			get
			{
				return (TargetMode & TargetFlags.Stations) != 0;
			}
			set
			{
				if (value)
				{
					TargetMode |= TargetFlags.Stations;
				}
				else
				{
					TargetMode &= ~TargetFlags.Stations;
				}
			}
		}

		public bool TargetNeutrals
		{
			get
			{
				return (TargetMode & TargetFlags.Neutrals) != 0;
			}
			set
			{
				if (value)
				{
					TargetMode |= TargetFlags.Neutrals;
				}
				else
				{
					TargetMode &= ~TargetFlags.Neutrals;
				}
			}
		}

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

			ElevationSpeedPerTick = ElevationSpeed * KeenRotationMultiplyer;
			AzimuthSpeedPerTick = RotationSpeed * KeenRotationMultiplyer;

			if (unknownIdleElevation)
			{
				Vector3.GetAzimuthAndElevation(gun.GunBase.GetMuzzleLocalMatrix().Forward, out idleAzimuth, out idleElevation);
				unknownIdleElevation = false;
			}

			Azimuth = idleAzimuth;
			Elevation = idleElevation;

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
					MatrixD muzzleMatrix = gun.GunBase.GetMuzzleWorldMatrix();

					string ammoId = gun.GunBase.CurrentAmmoDefinition.Id.SubtypeId.String;
					AmmoDefinition ammo = Settings.AmmoDefinitionLookup[ammoId];

					Vector3D leadPosition = CalculateProjectileInterceptPosition(ammo.DesiredSpeed, Block.CubeGrid.Physics.LinearVelocity, muzzleMatrix.Translation, Target.Physics.LinearVelocity, Target.PositionComp.GetPosition());

					LookAt(leadPosition);
				}

				RotateModels();
			}

			base.Animate();
		}

		public void TakeControl() 
		{ 
		
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

			float azimuthAngleDifference = targetAzimuth - Azimuth;
			float azimuthTravel = MathHelper.Clamp(azimuthAngleDifference, -AzimuthSpeedPerTick, AzimuthSpeedPerTick);

			if (Math.Abs(azimuthAngleDifference) < Math.PI)
				Azimuth += azimuthTravel;
			else
				Azimuth -= azimuthTravel;

			Elevation += MathHelper.Clamp(targetElevation - Elevation, -ElevationSpeedPerTick, ElevationSpeedPerTick);
			Azimuth = MathHelper.WrapAngle(Azimuth);

			//MyAPIGateway.Utilities.ShowNotification($"Azimuth: {Azimuth.ToString("n3")} Elevation: {Elevation.ToString("n3")} Diff: {azimuthAngleDifference.ToString("n5")}", 1);
		}

		// Whip's CalculateProjectileInterceptPosition Method
		// Uses vector math as opposed to the quadratic equation
		private static Vector3D CalculateProjectileInterceptPosition(double projectileSpeed, Vector3D shooterVelocity, Vector3D shooterPosition, Vector3D targetVelocity, Vector3D targetPos, double interceptPointMultiplier = 1)
		{
			var directHeading = targetPos - shooterPosition;
			var directHeadingNorm = Vector3D.Normalize(directHeading);

			var relativeVelocity = targetVelocity - shooterVelocity;

			var parallelVelocity = relativeVelocity.Dot(directHeadingNorm) * directHeadingNorm;
			var normalVelocity = relativeVelocity - parallelVelocity;

			var diff = projectileSpeed * projectileSpeed - normalVelocity.LengthSquared();
			if (diff < 0)
				return normalVelocity;

			var projectileForwardVelocity = Math.Sqrt(diff) * directHeadingNorm;
			var timeToIntercept = interceptPointMultiplier * Math.Abs(Vector3D.Dot(directHeading, directHeadingNorm)) / Vector3D.Dot(projectileForwardVelocity, directHeadingNorm);

			return shooterPosition + timeToIntercept * (projectileForwardVelocity + normalVelocity);
		}

		private void TargetAcquisition()
		{
			//target priority
			//missiles, suits, metior, decoys, closest terminal block
			MyGridTargeting targetingSystem = Block.CubeGrid.Components.Get<MyGridTargeting>();

			if (targetingSystem == null)
				return;

			List<MyEntity> targetSet = targetingSystem.TargetRoots;

			double likelyTargetDistanceSqrd = double.MaxValue;
			MyEntity likelyTarget = null;
			foreach (MyEntity t in targetSet)
			{
				
			}


			//try
			//{
			//	List<MyEntity> targets = targetingSystem.TargetRoots;

			//	Target = targets[0];

			//	//MyAPIGateway.Utilities.ShowNotification($"Target Count: {targets.Count}", 1);

			//}
			//catch
			//{ }
		}

		private bool IsNonGridTarget(MyEntity entity)
		{
			//if (entity is MyDebrisBase)
			//{
			//	return false;
			//}

			if (entity.Physics != null && !entity.Physics.Enabled)
			{
				return false;
			}

			if (entity is MyMeteor)
			{
				return TargetMeteors;
			}

			string typeName = entity.GetType().Name;
			if (typeName == "MyMissile")
			{
				return TargetMissiles;
			}

			// dont think this should be here
			if (entity is IMyDecoy)
			{
				return true;
			}

			IMyCharacter myCharacter = entity as IMyCharacter;
			if (myCharacter != null)
			{
				return TargetCharacters && !myCharacter.IsDead;
			}

			return false;
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
