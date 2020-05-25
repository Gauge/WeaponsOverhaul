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

		public AmmoDefinition Ammo;

		public Vector3D Origin;

		public Vector3 Direction;

		public Vector3D Velocity;

		public Vector3D Position;

		private bool DrawFullTracer;

		public Projectile(Vector3D origin, Vector3 direction, Vector3D startVelocity, AmmoDefinition ammo, long shooterId) 
		{
			Ammo = ammo;
			ShooterId = shooterId;

			Origin = origin;
			Position = origin;
			Direction = direction;

			float speed = ammo.DesiredSpeed + ((ammo.SpeedVariance > 0) ? MyRandom.Instance.GetRandomFloat(-ammo.SpeedVariance, ammo.SpeedVariance) : 0);
			Velocity = startVelocity + (Direction * speed);
		}

		public void Update() 
		{
			Check();

			Position += Velocity * Tools.Tick;
			if ((Origin - Position).LengthSquared() > Ammo.MaxTrajectory * Ammo.MaxTrajectory)
			{
				Expired = true;
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

				//apply recoil
				if (Ammo.ProjectileHitImpulse > 0)
				{
					Vector3 forceVector = -hit.Normal * Ammo.ProjectileHitImpulse;
					hit.HitEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, forceVector, hit.Position, Vector3.Zero);
				}

				if (!MyAPIGateway.Session.IsServer)
				{
					Expired = true;
					return;
				}

				if (hit.HitEntity is IMyDestroyableObject)
				{
					//Core.DamageRequests.Enqueue(new DamageDefinition {
					//	Victim = (hit.HitEntity as IMyDestroyableObject),
					//	Damage = Ammo.ProjectileMassDamage,
					//	DamageType = MyStringHash.GetOrCompute(Ammo.SubtypeId),
					//	ShooterId = ShooterId,
					//});
				}
				else if (hit.HitEntity is IMyCubeGrid)
				{
					IMyCubeGrid grid = hit.HitEntity as IMyCubeGrid;
					Vector3I? hitPos = grid.RayCastBlocks(hit.Position, hit.Position + Direction);
					if (hitPos.HasValue)
					{
						IMySlimBlock block = grid.GetCubeBlock(hitPos.Value);

						//Core.DamageRequests.Enqueue(new DamageDefinition {
						//	Victim = block,
						//	Damage = Ammo.ProjectileMassDamage,
						//	DamageType = MyStringHash.GetOrCompute(Ammo.SubtypeId),
						//	ShooterId = ShooterId,
						//});
					}
				}

				Expired = true;
			}
		}

		public void Draw()
		{
			// Most of this function was ripped from whiplash141's work.
			if (MyRandom.Instance.NextFloat() < Ammo.ProjectileTrailProbability)
			{
				float length = 0.6f * 40f * Ammo.ProjectileTrailScale;
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
				float thickness = (MyParticlesManager.Paused ? 0.2f : MyUtils.GetRandomFloat(0.2f, 0.3f)) * Ammo.ProjectileTrailScale;
				thickness *= MathHelper.Lerp(0.2f, 0.8f, 1f);

				MyStringId mat = string.IsNullOrWhiteSpace(Ammo.ProjectileTrailMaterial) ? MyStringId.GetOrCompute("ProjectileTrailLine") : MyStringId.GetOrCompute(Ammo.ProjectileTrailMaterial);

				MyTransparentGeometry.AddLineBillboard(mat, new Vector4(Ammo.ProjectileTrailColor * scaleFactor * 10f, 1f), start, Direction, length, thickness);
			}
		}
	}
}
