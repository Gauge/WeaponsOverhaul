using VRage.Game;
using VRage.Utils;
using VRageMath;
using VRage.Library.Utils;
using VRage.Game.ModAPI;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI.Interfaces;

namespace WeaponsOverhaul
{

	public class Projectile
	{
		public bool Expired;

		public long ShooterId;

		public string AmmoDefinitionId;

		public Vector3D Origin;

		public Vector3 Direction;

		public Vector3D Velocity;

		public Vector3D Position;

		private bool DrawFullTracer;

		public Projectile(long shooterId, Vector3D origin, Vector3D direction, Vector3D initialVelocity, string ammoDefinitionId)
		{
			ShooterId = shooterId;
			AmmoDefinitionId = ammoDefinitionId;
			Origin = origin;
			Direction = direction;

			AmmoDefinition ammo = Settings.AmmoDefinitionLookup[AmmoDefinitionId];

			Position = Origin;
			Velocity = initialVelocity + (direction * ammo.DesiredSpeed);

		}

		public void Update() 
		{
			AmmoDefinition ammo = Settings.AmmoDefinitionLookup[AmmoDefinitionId];

			Check();

			Position += Velocity * Tools.Tick;
			if ((Origin - Position).LengthSquared() > ammo.MaxTrajectory * ammo.MaxTrajectory)
			{
				Core.ExpireProjectile(this);
			}
		}

		private void Check() 
		{
			Vector3D End = Position + Velocity * Tools.Tick;

			IHitInfo hit;
			MyAPIGateway.Physics.CastRay(Position, End, out hit);
			if (hit != null)
			{
				Expired = true;
				AmmoDefinition ammo = Settings.AmmoDefinitionLookup[AmmoDefinitionId];

				//apply recoil
				if (ammo.BackkickForce > 0)
				{
					Vector3 forceVector = -hit.Normal * ammo.ProjectileHitImpulse;
					hit.HitEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, forceVector, hit.Position, Vector3.Zero);
				}

				if (!MyAPIGateway.Session.IsServer)
				{
					Core.ExpireProjectile(this);
					return;
				}

				if (hit.HitEntity is IMyDestroyableObject)
				{
					Core.DamageRequests.Enqueue(new DamageDefinition {
						Victim = (hit.HitEntity as IMyDestroyableObject),
						Damage = ammo.ProjectileMassDamage,
						DamageType = MyStringHash.GetOrCompute(ammo.SubtypeId),
						ShooterId = ShooterId,
					});
				}
				else if (hit.HitEntity is IMyCubeGrid)
				{
					IMyCubeGrid grid = hit.HitEntity as IMyCubeGrid;
					Vector3I? hitPos = grid.RayCastBlocks(hit.Position, hit.Position + Direction);
					if (hitPos.HasValue)
					{
						IMySlimBlock block = grid.GetCubeBlock(hitPos.Value);

						Core.DamageRequests.Enqueue(new DamageDefinition {
							Victim = block,
							Damage = ammo.ProjectileMassDamage,
							DamageType = MyStringHash.GetOrCompute(ammo.SubtypeId),
							ShooterId = ShooterId,
						});
					}
				}

				Core.ExpireProjectile(this);
			}
		}

		public void Draw()
		{
			// Most of this function was ripped from whiplash141's work.

			AmmoDefinition ammo = Settings.AmmoDefinitionLookup[AmmoDefinitionId];

			if (MyRandom.Instance.NextFloat() < ammo.ProjectileTrailProbability)
			{
				float length = 0.6f * 40f * ammo.ProjectileTrailScale;
				Vector3D start;
				if (DrawFullTracer)
				{
					start = Position - (Direction * length);
				}
				else
				{
					float distance = (float)Vector3D.Distance(Origin, Position);
					if (length <= distance)
					{
						DrawFullTracer = true;
						start = Position - (Direction * length);
					}
					else
					{
						start = Origin;
						length = distance;
					} 

				} 

				float scaleFactor = MyParticlesManager.Paused ? 1f : MyUtils.GetRandomFloat(1f, 2f);
				float thickness = (MyParticlesManager.Paused ? 0.2f : MyUtils.GetRandomFloat(0.2f, 0.3f)) * ammo.ProjectileTrailScale;
				thickness *= MathHelper.Lerp(0.2f, 0.8f, 1f);

				MyStringId mat = string.IsNullOrWhiteSpace(ammo.ProjectileTrailMaterial) ? MyStringId.GetOrCompute("ProjectileTrailLine") : MyStringId.GetOrCompute(ammo.ProjectileTrailMaterial);

				MyTransparentGeometry.AddLineBillboard(mat, new Vector4(ammo.ProjectileTrailColor * scaleFactor * 10f, 1f), start, Direction, length, thickness);
			}
		}
	}
}
