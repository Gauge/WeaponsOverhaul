using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
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

		private TargetFlags TargetMode = TargetFlags.LargeShips | TargetFlags.SmallShips | TargetFlags.Characters | TargetFlags.Stations;

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
			MaxAzimuthRadians = MathHelper.ToRadians(Tools.NormalizeAngle(MaxAzimuthDegrees));
			if (MinAzimuthRadians > MaxAzimuthRadians)
			{
				MinAzimuthRadians -= MathHelper.Pi * 2f;
			}

			ElevationSpeedPerTick = ElevationSpeed * KeenRotationMultiplyer;
			AzimuthSpeedPerTick = RotationSpeed * KeenRotationMultiplyer;

			if (unknownIdleElevation)
			{
				GetIdleAzimuthAndElevation(out idleAzimuth, out idleElevation);
			}

			Azimuth = idleAzimuth;
			Elevation = idleElevation;

		}

		public override void Update()
		{
			if (AiEnabled)
			{
				if (Target == null)
				{
					Target = TargetAcquisition();
				}

				if (Target != null && (Target.Closed || Target.MarkedForClose))
				{
					Target = null;
				}

				if (Target != null)
				{
					MatrixD muzzleMatrix = gun.GunBase.GetMuzzleWorldMatrix();

					string ammoId = gun.GunBase.CurrentAmmoDefinition.Id.SubtypeId.String;
					AmmoDefinition ammo = Settings.AmmoDefinitionLookup[ammoId];

					Tools.AddGPS(Target.EntityId + 3, muzzleMatrix.Translation);

					Vector3D linearVelocity;
					if (Target is IMyCubeBlock)
					{
						linearVelocity = (Target as MyCubeBlock).CubeGrid.Physics.LinearVelocity;
					}
					else
					{
						linearVelocity = Target.Physics.LinearVelocity;
					}

					Vector3D targetPosition;
					if (Target is IMyCharacter)
					{
						targetPosition = Target.PositionComp.WorldVolume.Center + (Target.WorldMatrix.Up * 0.5f);
						Tools.AddGPS(Target.EntityId, targetPosition);
					}
					else
					{
						targetPosition = Target.PositionComp.WorldAABB.Center;
					}

					Vector3D leadPosition = CalculateProjectileInterceptPosition(ammo.DesiredSpeed, Block.CubeGrid.Physics.LinearVelocity, muzzleMatrix.Translation, linearVelocity, targetPosition);

					Tools.AddGPS(Target.EntityId + 2, leadPosition);

					float remainingAngle = LookAt(leadPosition);

					if (Math.Abs(remainingAngle) < 0.02)
					{
						State.Value |= WeaponState.AIShoot;
					}
					else
					{
						State.Value &= ~WeaponState.AIShoot;
					}

					MyAPIGateway.Utilities.ShowNotification($"{Target.GetType().Name} {(CubeBlock.WorldMatrix.Translation - Target.WorldMatrix.Translation).Length().ToString("n0")} - {remainingAngle.ToString("n5")} - {linearVelocity.ToString("n2")}", 1);
				}
			}


			base.Update();
		}

		public override void Animate()
		{
			RotateModels();


			base.Animate();
		}

		public void TakeControl()
		{

		}

		private void GetIdleAzimuthAndElevation(out float idleAzimuth, out float idleElevation)
		{
			Vector3.GetAzimuthAndElevation(gun.GunBase.GetMuzzleLocalMatrix().Forward, out idleAzimuth, out idleElevation);
			unknownIdleElevation = false;
		}

		/// <summary>
		/// Adjusts azimuth and elevation over time to face the target within turret limits
		/// </summary>
		/// <returns>the angle remaining before the turret is facing the target</returns>
		private float LookAt(Vector3D target)
		{
			float az = 0;
			float elev = 0;

			Vector3D muzzleWorldPosition = gun.GunBase.GetMuzzleWorldPosition();
			Vector3.GetAzimuthAndElevation(Vector3.Normalize(Vector3D.TransformNormal(target - muzzleWorldPosition, CubeBlock.PositionComp.WorldMatrixInvScaled)), out az, out elev);

			if (unknownIdleElevation)
			{
				GetIdleAzimuthAndElevation(out idleAzimuth, out idleElevation);
			}

			float targetElevation = elev - idleElevation;
			float targetAzimuth = az - idleAzimuth;

			float elevationAngleDifference = targetElevation - Elevation;
			float azimuthAngleDifference = targetAzimuth - Azimuth;

			float azimuthTravel = MathHelper.Clamp(azimuthAngleDifference, -AzimuthSpeedPerTick, AzimuthSpeedPerTick);

			if (Math.Abs(azimuthAngleDifference) < Math.PI)
				Azimuth += azimuthTravel;
			else
				Azimuth -= azimuthTravel;

			Elevation += MathHelper.Clamp(targetElevation - Elevation, -ElevationSpeedPerTick, ElevationSpeedPerTick);
			Azimuth = MathHelper.WrapAngle(Azimuth);

			return (azimuthAngleDifference > elevationAngleDifference) ? azimuthAngleDifference : elevationAngleDifference;
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

		/// <summary>
		/// target priority
		/// missiles - not implementing due to keen
		/// suites, decoys
		/// closest terminal block
		/// meteor
		/// </summary>
		private MyEntity TargetAcquisition()
		{
			MyGridTargeting targetingSystem = Block.CubeGrid.Components.Get<MyGridTargeting>();
			if (targetingSystem == null)
				return null;

			List<MyEntity> targetSet = targetingSystem.TargetRoots;

			float range = (CubeBlock as IMyLargeTurretBase).Range;
			float maximumRangeSqrd = range * range;

			double likelyTargetDistanceSqrd = double.MaxValue;
			MyEntity likelyTarget = null;
			foreach (MyEntity ent in targetSet)
			{
				if (ent.Physics == null || !ent.Physics.Enabled)
				{
					continue;
				}

				IMyCharacter myCharacter = ent as IMyCharacter;
				if (myCharacter != null)
				{
					if (!TargetCharacters || myCharacter.IsDead) // dont target if not set or dead
						continue;

					IMyPlayer player = MyAPIGateway.Players.GetPlayerControllingEntity(ent);
					MyRelationsBetweenPlayerAndBlock relation = player.GetRelationTo(CubeBlock.OwnerId);

					if (relation == MyRelationsBetweenPlayerAndBlock.Enemies ||
						TargetNeutrals && (relation == MyRelationsBetweenPlayerAndBlock.Neutral || relation == MyRelationsBetweenPlayerAndBlock.NoOwnership))
					{
						double rangeSqrd = (CubeBlock.WorldMatrix.Translation - ent.WorldMatrix.Translation).LengthSquared();
						if (rangeSqrd < maximumRangeSqrd && rangeSqrd < likelyTargetDistanceSqrd)
						{
							likelyTarget = ent;
							likelyTargetDistanceSqrd = rangeSqrd;
						}
					}
				}

				if (likelyTarget is IMyCharacter)
					continue;

				MyCubeGrid grid = ent as MyCubeGrid;
				if (grid != null)
				{
					if (grid.GridSizeEnum == MyCubeSize.Large)
					{
						if (!TargetLargeShips || (grid.IsStatic && !TargetStations))
							continue;
					}
					else
					{
						if (!TargetSmallShips)
							continue;
					}

					foreach (MyCubeBlock block in grid.GetFatBlocks())
					{
						if (likelyTarget is IMyDecoy && !(block is IMyDecoy))
							continue;

						MyRelationsBetweenPlayerAndBlock relation = block.GetUserRelationToOwner(CubeBlock.OwnerId);

						if (relation == MyRelationsBetweenPlayerAndBlock.Enemies ||
							TargetNeutrals && (relation == MyRelationsBetweenPlayerAndBlock.Neutral || relation == MyRelationsBetweenPlayerAndBlock.NoOwnership))
						{
							double rangeSqrd = (CubeBlock.WorldMatrix.Translation - block.WorldMatrix.Translation).LengthSquared();
							if (rangeSqrd < maximumRangeSqrd &&
								(rangeSqrd < likelyTargetDistanceSqrd || (block is IMyDecoy && !(likelyTarget is IMyDecoy))))
							{
								likelyTarget = block;
								likelyTargetDistanceSqrd = rangeSqrd;
							}
						}
					}
				}

				if (likelyTarget is IMyCubeBlock)
					continue;

				if (ent is MyMeteor)
				{
					if (!TargetMeteors)
						continue;

					double rangeSqrd = (CubeBlock.WorldMatrix.Translation - ent.WorldMatrix.Translation).LengthSquared();
					if (rangeSqrd < maximumRangeSqrd && rangeSqrd < likelyTargetDistanceSqrd)
					{
						likelyTarget = ent;
						likelyTargetDistanceSqrd = rangeSqrd;
					}
				}
			}

			return likelyTarget;
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
