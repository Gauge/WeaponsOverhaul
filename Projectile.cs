using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Utils;
using VRageMath;

namespace WeaponsOverhaul
{
	[ProtoContract]
	public class Projectile
	{
		[XmlIgnore]
		public static MyStringId DefaultProjectileTrail = MyStringId.GetOrCompute("ProjectileTrailLine");
		[XmlIgnore]
		public Vector3D VelocityPerTick => Velocity * Tools.Tick;
		[XmlIgnore]
		public bool IsAtRange => DistanceTraveled * LifeTimeTicks > Ammo.MaxTrajectory * Ammo.MaxTrajectory;
		[XmlIgnore]
		public bool UseLongRaycast => Ammo.DesiredSpeed * Tools.Tick * CollisionCheckFrames > 50;

		[XmlIgnore]
		public IMySlimBlock PartentSlim;
		[XmlIgnore]
		public bool Initialized = false;

		[ProtoMember(10)]
		public long ParentBlockId;
		[ProtoMember(11)]
		public string AmmoId;
		[ProtoMember(20)]
		public Vector3D Position;
		[ProtoMember(30)]
		public Vector3D Velocity;
		[XmlIgnore]
		public Vector3D Direction;
		[ProtoMember(40)]
		public Vector3D InitialGridVelocity;
		[XmlIgnore]
		public double DistanceTraveled;

		[XmlIgnore]
		AmmoDefinition Ammo;
		[XmlIgnore]
		public int LifeTimeTicks;
		[XmlIgnore]
		public bool HasExpired;
		[XmlIgnore]
		public float LastPositionFraction = 0;
		[XmlIgnore]
		public Vector3D PreviousPosition;
		[XmlIgnore]
		public Vector3D Start;
		[XmlIgnore]
		public Vector3D End;

		[XmlIgnore]
		public int CollisionCheckFrames = -1;
		[XmlIgnore]
		public int CollisionCheckCounter = 0;
		[XmlIgnore]
		public bool DoShortRaycast = false;

		/// <summary>
		/// Initializes all empty variables
		/// </summary>
		public virtual void Init()
		{
			if (Settings.AmmoDefinitionLookup.ContainsKey(AmmoId))
			{
				Ammo = Settings.AmmoDefinitionLookup[AmmoId];
			}

			if (Direction == null)
			{
				Direction = Vector3D.Normalize(Velocity);
			}

			Initialized = true;
		}

		/// <summary>
		/// This is the first function call in the projectile life cycle loop
		/// Updates LifetimeTicks that keeps track of distance traveled
		/// </summary>
		public virtual void PreUpdate()
		{
			PreviousPosition = Position;
			LifeTimeTicks++;
		}

		/// <summary>
		/// This happens after collision check
		/// Updates the position of the projectile
		/// </summary>
		public virtual void Update()
		{
			Position += VelocityPerTick;
			DistanceTraveled += VelocityPerTick.LengthSquared();

			if (IsAtRange)
			{
				HasExpired = true;
			}
		}

		/// <summary>
		/// Draws the projectile
		/// </summary>
		public virtual void Draw()
		{
			float length = 0.6f * 40f * Ammo.ProjectileTrailScale;
			Vector3D startPoint = End - Direction * length;

			float scaleFactor = MyParticlesManager.Paused ? 1f : MyUtils.GetRandomFloat(1f, 2f);
			float thickness = (MyParticlesManager.Paused ? 0.2f : MyUtils.GetRandomFloat(0.2f, 0.3f)) * Ammo.ProjectileTrailScale;
			thickness *= MathHelper.Lerp(0.2f, 0.8f, 1f);

			MyTransparentGeometry.AddLineBillboard(
					string.IsNullOrWhiteSpace(Ammo.ProjectileTrailMaterial) ? DefaultProjectileTrail : MyStringId.GetOrCompute(Ammo.ProjectileTrailMaterial),
					new Vector4(Ammo.ProjectileTrailColor * 10f, 1f),
					Position - (Direction * length),
					-Direction,
					length,
					thickness);
		}

		/// <summary>
		/// Define collision start and end points and other precalculation operations
		/// </summary>
		public virtual void PreCollitionDetection()
		{
			Start = Position;
			if (DoShortRaycast)
			{
				End = Position + VelocityPerTick;
				DoShortRaycast = false;
			}
			else
			{
				End = Position + (VelocityPerTick * CollisionCheckFrames);
			}
		}

		/// <summary>
		/// Checks for collisions
		/// </summary>
		public void CollisionDetection()
		{
			IHitInfo hit = null;
			List<IHitInfo> hitlist = new List<IHitInfo>();
			if (UseLongRaycast)
			{
				MyAPIGateway.Physics.CastLongRay(Start, End, out hit, false);
			}
			else
			{
				MyAPIGateway.Physics.CastRay(Start, End, hitlist);

				if (hitlist.Count > 0)
				{
					hit = hitlist[0];
				}
			}

			if (hit != null && hit.Position != null)
			{
				int framesToWait = (int)Math.Floor(hit.Fraction * CollisionCheckFrames);
				if (framesToWait < 1)
				{
					HasExpired = true;
					if (!MyAPIGateway.Session.IsServer)
						return;

					if (hit.HitEntity is IMyDestroyableObject)
					{
						IMyDestroyableObject obj = hit.HitEntity as IMyDestroyableObject;

						Core.DamageRequests.Enqueue(new DamageDefinition {
							Victim = (hit.HitEntity as IMyDestroyableObject),
							Damage = Ammo.ProjectileHealthDamage,
							DamageType = MyStringHash.GetOrCompute(Ammo.SubtypeId),
							Sync = true,
							Hit = default(MyHitInfo),
							AttackerId = ParentBlockId,
							ImpulseEntity = hit.HitEntity,
							ImpulseForce = (Direction * Ammo.ProjectileHitImpulse),
							ImpulsePosition = hit.Position
						});

						LastPositionFraction = hit.Fraction;
					}
					else if (hit.HitEntity is IMyCubeGrid)
					{
						IMyCubeGrid grid = hit.HitEntity as IMyCubeGrid;

						Vector3D direction = Direction;
						Vector3I? hitPos = grid.RayCastBlocks(hit.Position, hit.Position + direction);
						if (hitPos.HasValue)
						{
							IMySlimBlock block = grid.GetCubeBlock(hitPos.Value);
							//if (IgnoreDamageReduction)
							//{
							//	float mult = Tools.GetScalerInverse(((MyCubeBlockDefinition)block.BlockDefinition).GeneralDamageMultiplier);

							//	Core.DamageRequests.Enqueue(new DamageDefinition {
							//		Victim = block,
							//		Damage = ProjectileMassDamage * mult,
							//		DamageType = MyStringHash.GetOrCompute(SubtypeId),
							//		Sync = true,
							//		Hit = default(MyHitInfo),
							//		AttackerId = ParentBlockId,
							//		ImpulseEntity = hit.HitEntity,
							//		ImpulseForce = (Direction * ProjectileHitImpulse),
							//		ImpulsePosition = hit.Position
							//	});
							//}
							//else
							//{
								Core.DamageRequests.Enqueue(new DamageDefinition {
									Victim = block,
									Damage = Ammo.ProjectileMassDamage,
									DamageType = MyStringHash.GetOrCompute(Ammo.SubtypeId),
									Sync = true,
									Hit = default(MyHitInfo),
									AttackerId = ParentBlockId,
									ImpulseEntity = hit.HitEntity,
									ImpulseForce = (Direction * Ammo.ProjectileHitImpulse),
									ImpulsePosition = hit.Position
								});
							//}

							LastPositionFraction = hit.Fraction;
						}
					}
				}
				else
				{
					CollisionCheckCounter = CollisionCheckWaitFrames() - framesToWait;
					DoShortRaycast = true;
				}
			}
		}

		public bool DoCollisionCheck()
		{
			if (HasExpired)
			{
				return false;
			}

			CollisionCheckCounter++;
			if (CollisionCheckCounter != CollisionCheckWaitFrames())
			{
				return false;
			}
			else
			{
				CollisionCheckCounter = 0;
				return true;
			}
		}

		public int CollisionCheckWaitFrames()
		{
			if (CollisionCheckFrames == -1)
			{
				CollisionCheckFrames = 1 + (int)Math.Ceiling((Ammo.DesiredSpeed / Tools.MaxSpeedLimit) * 0.5f);
			}

			CollisionCheckFrames = 1;
			return CollisionCheckFrames;
		}

		public void ResetCollisionCheck()
		{
			CollisionCheckFrames = -1;
		}
	}
}
