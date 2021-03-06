﻿using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Collections;
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

		public float Azimuth;
		public float Elevation;

		private float IdleAzimuth = 0;
		private float IdleElevation = 0;
		private bool unknownIdleElevation = true;

		private float AzimuthSpeedPerTick;
		private float ElevationSpeedPerTick;

		private float MinElevationRadians;
		private float MaxElevationRadians = MathHelper.Pi * 2f;
		private float MinAzimuthRadians;
		private float MaxAzimuthRadians = MathHelper.Pi * 2f;

		private bool IsAzimuthLimited;
		private bool IsElevationLimited;

		private float MaximumRangeSqrd = 0;

		private MyEntitySubpart modelBase1;
		private MyEntitySubpart modelBase2;
		private MyEntitySubpart modelBarrel;

		private MyEntity Target;

		private TargetFlags TargetMode = TargetFlags.LargeShips | TargetFlags.SmallShips | TargetFlags.Characters | TargetFlags.Stations | TargetFlags.Meteors;

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

		public override void Start()
		{
			base.Start();

			InitializeTurretControls();
		}

		public override void SystemRestart()
		{
			base.SystemRestart();

			InitializeTurretControls();
		}

		private void InitializeTurretControls()
		{
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
				Vector3.GetAzimuthAndElevation(gun.GunBase.GetMuzzleLocalMatrix().Forward, out IdleAzimuth, out IdleElevation);
				unknownIdleElevation = false;
			}

			Azimuth = IdleAzimuth;
			Elevation = IdleElevation;

			IsAzimuthLimited = Math.Abs(MaxAzimuthRadians - MinAzimuthRadians - MathHelper.Pi * 2f) > 0.01f;
			IsElevationLimited = Math.Abs(MaxElevationRadians - MinElevationRadians - MathHelper.Pi * 2f) > 0.01f;
		}

		public override void Update()
		{
			if (AiEnabled && CubeBlock.IsWorking)
			{
				//update per frame variables
				MaximumRangeSqrd = (CubeBlock as IMyLargeTurretBase).Range;
				MaximumRangeSqrd = MaximumRangeSqrd * MaximumRangeSqrd;

				UpdateAI();
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

		private void UpdateAI()
		{
			if (!IsValidTarget(Target))
			{
				IsAIShooting = false;
				Target = TargetAcquisition();
			}

			if (Target != null)
			{
				MatrixD muzzleMatrix = gun.GunBase.GetMuzzleWorldMatrix();
				string ammoId = gun.GunBase.CurrentAmmoDefinition.Id.SubtypeId.String;
				AmmoDefinition ammo = Settings.AmmoDefinitionLookup[ammoId];

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
				}
				else
				{
					targetPosition = Target.PositionComp.WorldAABB.Center;
				}

				Vector3D leadPosition = CalculateProjectileInterceptPosition(ammo.DesiredSpeed, Block.CubeGrid.Physics.LinearVelocity, muzzleMatrix.Translation, linearVelocity, targetPosition);

				if (IsTargetVisible(Target, leadPosition))
				{
					Tools.AddGPS(CubeBlock.EntityId + 2, leadPosition);
					float remainingAngle = LookAt(leadPosition);
					// does not shoot if the angle (radians) is not close to on target
					IsAIShooting = Math.Abs(remainingAngle) < 0.02;
				}
				else
				{
					Target = null;
				}

				//MyAPIGateway.Utilities.ShowNotification($"{Target.GetType().Name} {(CubeBlock.WorldMatrix.Translation - Target.WorldMatrix.Translation).Length().ToString("n0")} - {remainingAngle.ToString("n5")} - {linearVelocity.ToString("n2")}", 1);
			}
		}

		private class AzimuthAndElevation
		{
			public float Azimuth;
			public float Elevation;
		}

		private float LookAt(Vector3D target)
		{
			AzimuthAndElevation ori = GetTargetAzimuthElevation(target);

			float elevationAngleDifference = ori.Elevation - Elevation;
			float azimuthAngleDifference = ori.Azimuth - Azimuth;

			float azimuthTravel = MathHelper.Clamp(azimuthAngleDifference, -AzimuthSpeedPerTick, AzimuthSpeedPerTick);

			if (Math.Abs(azimuthAngleDifference) < Math.PI)
				Azimuth = Azimuth + azimuthTravel;
			else
				Azimuth = Azimuth - azimuthTravel;

			Elevation = Elevation + MathHelper.Clamp(elevationAngleDifference, -ElevationSpeedPerTick, ElevationSpeedPerTick);
			Azimuth = MathHelper.WrapAngle(Azimuth);

			return (azimuthAngleDifference > elevationAngleDifference) ? azimuthAngleDifference : elevationAngleDifference;
		}

		private AzimuthAndElevation GetTargetAzimuthElevation(Vector3D target)
		{
			float az = 0;
			float elev = 0;
			Vector3D muzzleWorldPosition = gun.GunBase.GetMuzzleWorldPosition();
			Vector3.GetAzimuthAndElevation(Vector3.Normalize(Vector3D.TransformNormal(target - muzzleWorldPosition, CubeBlock.PositionComp.WorldMatrixInvScaled)), out az, out elev);

			return new AzimuthAndElevation() {
				Azimuth = MathHelper.WrapAngle(az - IdleAzimuth),
				Elevation = elev - IdleElevation,
			};
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

			double likelyTargetDistanceSqrd = double.MaxValue;
			MyEntity likelyTarget = null;
			foreach (MyEntity ent in targetSet)
			{
				if (ent.Physics == null || !ent.Physics.Enabled)
				{
					continue;
				}

				if (IsValidCharacter(ent as IMyCharacter))
				{
					Vector3D characterPosition = ent.PositionComp.WorldVolume.Center + (ent.WorldMatrix.Up * 0.5f);
					double rangeSqrd = (CubeBlock.WorldMatrix.Translation - characterPosition).LengthSquared();
					if (rangeSqrd < MaximumRangeSqrd && rangeSqrd < likelyTargetDistanceSqrd && // is closest
						IsPositionInTurretArch(characterPosition) && // is in arch
						IsTargetVisible(ent, characterPosition)) // is visible
					{
						likelyTarget = ent;
						likelyTargetDistanceSqrd = rangeSqrd;
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

					bool foundEnemyGrid = false;
					if (grid.BigOwners.Count == 0 && TargetNeutrals)
					{
						foundEnemyGrid = true;
					}

					foreach (long owner in grid.BigOwners)
					{
						MyRelationsBetweenPlayerAndBlock relation = CubeBlock.GetUserRelationToOwner(owner);

						if (relation == MyRelationsBetweenPlayerAndBlock.Enemies ||
							TargetNeutrals && (relation == MyRelationsBetweenPlayerAndBlock.Neutral || relation == MyRelationsBetweenPlayerAndBlock.NoOwnership))
						{
							foundEnemyGrid = true;
						}
					}

					if (!foundEnemyGrid)
						continue;

					foreach (MyCubeBlock block in grid.GetFatBlocks())
					{
						if (likelyTarget is IMyDecoy)
							break;

						if (likelyTarget != null && !(block is IMyDecoy))
							continue;

						MyRelationsBetweenPlayerAndBlock relation = block.GetUserRelationToOwner(CubeBlock.OwnerId);

						if (relation == MyRelationsBetweenPlayerAndBlock.Enemies ||
							TargetNeutrals && (relation == MyRelationsBetweenPlayerAndBlock.Neutral || relation == MyRelationsBetweenPlayerAndBlock.NoOwnership))
						{
							Vector3D blockPosition = block.WorldMatrix.Translation;
							double rangeSqrd = (CubeBlock.WorldMatrix.Translation - blockPosition).LengthSquared();
							if (rangeSqrd < MaximumRangeSqrd &&
								(rangeSqrd < likelyTargetDistanceSqrd || (block is IMyDecoy && !(likelyTarget is IMyDecoy))) &&
								IsPositionInTurretArch(blockPosition) &&
								IsTargetVisible(block, blockPosition))
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
					if (rangeSqrd < MaximumRangeSqrd && rangeSqrd < likelyTargetDistanceSqrd)
					{
						if (IsTargetVisible(ent, ent.WorldMatrix.Translation))
						{
							likelyTarget = ent;
							likelyTargetDistanceSqrd = rangeSqrd;
						}
					}
				}
			}

			return likelyTarget;
		}

		private bool IsTargetVisible(MyEntity target, Vector3D predictedPosition)
		{
			if (target == null)
			{
				return false;
			}
			if (target.GetTopMostParent()?.Physics == null)
			{
				return false;
			}

			Vector3D from = gun.GunBase.GetMuzzleWorldPosition() + gun.GunBase.WorldMatrix.Forward * 0.5;

			List<IHitInfo> hits = new List<IHitInfo>();
			MyAPIGateway.Physics.CastRay(from, predictedPosition, hits, 15);

			foreach (IHitInfo hit in hits)
			{
				if (hit != null && hit.HitEntity != null)
				{
					if (target == hit.HitEntity || target.Parent == hit.HitEntity || (target.Parent != null && target.Parent == hit.HitEntity.Parent) || hit.HitEntity is MyFloatingObject || hit.HitEntity.GetType().Name == "MyMissile")
					{
						continue;
					}

					return false;
				}
			}

			return true;
		}

		private bool IsValidTarget(MyEntity ent)
		{
			if (ent == null || ent.Closed || ent.MarkedForClose)
				return false;

			double distanceSqrd = (CubeBlock.WorldMatrix.Translation - ent.WorldMatrix.Translation).LengthSquared();
			if (distanceSqrd > MaximumRangeSqrd)
				return false;

			if (!IsPositionInTurretArch(ent.WorldMatrix.Translation))
				return false;

			return IsValidCharacter(ent as IMyCharacter) || IsValidCubeBlock(ent as IMyCubeBlock) || IsValidMeteor(ent as MyMeteor);
		}

		private bool IsPositionInTurretArch(Vector3D point)
		{
			AzimuthAndElevation ori = GetTargetAzimuthElevation(point);

			if (IsAzimuthLimited)
			{
				if (ori.Azimuth > MaxAzimuthRadians || ori.Azimuth < MinAzimuthRadians)
				{
					return false;
				}
			}

			if (IsElevationLimited)
			{
				if (ori.Elevation > MaxElevationRadians || ori.Elevation < MinElevationRadians)
				{
					return false;
				}
			}

			return true;
		}

		private bool IsValidCharacter(IMyCharacter character)
		{
			if (character != null)
			{
				if (!TargetCharacters || character.IsDead) // dont target if not set or dead
					return false;

				IMyPlayer player = MyAPIGateway.Players.GetPlayerControllingEntity(character);

				if (player == null)
					return false;

				MyRelationsBetweenPlayerAndBlock relation = player.GetRelationTo(CubeBlock.OwnerId);

				if (!(relation == MyRelationsBetweenPlayerAndBlock.Enemies ||
					TargetNeutrals && (relation == MyRelationsBetweenPlayerAndBlock.Neutral || relation == MyRelationsBetweenPlayerAndBlock.NoOwnership)))
				{
					return false;
				}

				return true;
			}

			return false;
		}

		private bool IsValidCubeBlock(IMyCubeBlock block)
		{
			if (block != null)
			{
				if (block.CubeGrid.GridSizeEnum == MyCubeSize.Large)
				{
					if (!TargetLargeShips || (block.CubeGrid.IsStatic && !TargetStations))
						return false;
				}
				else
				{
					if (!TargetSmallShips)
						return false;
				}

				MyRelationsBetweenPlayerAndBlock relation = block.GetUserRelationToOwner(CubeBlock.OwnerId);
				if (!(relation == MyRelationsBetweenPlayerAndBlock.Enemies ||
					TargetNeutrals && (relation == MyRelationsBetweenPlayerAndBlock.Neutral || relation == MyRelationsBetweenPlayerAndBlock.NoOwnership)))
				{
					return false;
				}

				return true;
			}

			return false;
		}

		private bool IsValidMeteor(MyMeteor meteor)
		{
			if (meteor != null)
			{
				return !TargetMeteors;
			}

			return false;
		}

		protected void RotateModels()
		{
			if (modelBase1 == null || modelBase2 == null || modelBarrel == null)
			{
				if (CubeBlock.Subparts.ContainsKey("GatlingTurretBase1"))
				{
					modelBase1 = CubeBlock.Subparts["GatlingTurretBase1"];
					modelBase2 = modelBase1.Subparts["GatlingTurretBase2"];
					modelBarrel = modelBase2.Subparts["GatlingBarrel"];

					if (!modelBase1.Render.IsChild(0))
					{
						ControlLayer.Entity.NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
						return;
					}
				}
				else
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

			if (IsAzimuthLimited)
			{
				Azimuth = Math.Min(MaxAzimuthRadians, Math.Max(MinAzimuthRadians, Azimuth));
			}

			if (IsElevationLimited)
			{
				Elevation = Math.Min(MaxElevationRadians, Math.Max(MinElevationRadians, Elevation));
			}
		}
	}
}
