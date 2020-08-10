using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Utils;
using VRageMath;

namespace WeaponsOverhaul.Definitions
{
    [ProtoContract]
	public class RicochetDefinition : ICollision
	{

        public bool Enabled { get; set; }

        private float deflectionAngle;
        [ProtoMember(1)]
        public float DeflectionAngle
        {
            get { return deflectionAngle; }
            set
            {
                if (value < 0)
                {
                    deflectionAngle = 0;
                }
                else if (value > 90)
                {
                    deflectionAngle = 90;
                }
                else
                {
                    deflectionAngle = value;
                }
            }
        }

        private float maxVelocityTransfer;
        [ProtoMember(2)]
        public float MaxVelocityTransfer
        {
            get { return maxVelocityTransfer; }
            set
            {
                if (value < 0)
                {
                    maxVelocityTransfer = 0;
                }
                else if (value > 1)
                {
                    maxVelocityTransfer = 1;
                }
                else
                {
                    maxVelocityTransfer = value;
                }
            }
        }

        private float maxDamageTransfer;
        [ProtoMember(3)]
        public float MaxDamageTransfer
        {
            get { return maxDamageTransfer; }
            set
            {
                if (value < 0)
                {
                    maxDamageTransfer = 0;
                }
                else if (value > 1)
                {
                    maxDamageTransfer = 1;
                }
                else
                {
                    maxDamageTransfer = value;
                }
            }
        }

        private float ricochetChance;
        [ProtoMember(4)]
        public float RicochetChance
        {
            get { return ricochetChance; }
            set
            {
                if (value < 0)
                {
                    ricochetChance = 0;
                }
                else if (value > 1)
                {
                    ricochetChance = 1;
                }
                else
                {
                    ricochetChance = value;
                }
            }
        }

        public RicochetDefinition Clone()
        {
            return new RicochetDefinition {
                DeflectionAngle = DeflectionAngle,
                MaxDamageTransfer = MaxDamageTransfer,
                MaxVelocityTransfer = MaxVelocityTransfer,
                RicochetChance = RicochetChance
            };
        }

        public void Check(Projectile p)
        {
            //base.Check(p);

            Vector3D End = p.Position + p.Velocity * Tools.Tick;

           

            IHitInfo hit;
            MyAPIGateway.Physics.CastRay(p.Position, End, out hit);
            if (hit != null)
            {
                if (hit.HitEntity is IMyDestroyableObject)
                {
                    p.Expired = true;
                    (hit.HitEntity as IMyDestroyableObject).DoDamage(p.Ammo.ProjectileMassDamage, MyStringHash.GetOrCompute(p.Ammo.SubtypeId), false, null, p.ShooterId);
                }
                else if (hit.HitEntity is IMyCubeGrid)
                {
                    IMyCubeGrid grid = hit.HitEntity as IMyCubeGrid;

                    Vector3I? hitPos = grid.RayCastBlocks(hit.Position, hit.Position + Vector3D.Normalize(p.Velocity)); // p.Direction
                    if (hitPos.HasValue)
                    {
                        IMySlimBlock block = grid.GetCubeBlock(hitPos.Value);


                        Vector3 hitObjectVelocity = Vector3.Zero;
                        if (hit.HitEntity.Physics != null)
                        {
                            hitObjectVelocity = hit.HitEntity.Physics.LinearVelocity;
                        }

                        Vector3D relativeV = p.Velocity - hitObjectVelocity;
                        float NotHitAngle = (float)Tools.AngleBetween(-Vector3D.Normalize(relativeV), hit.Normal);
                        float HitAngle = (90f - NotHitAngle);
                        float NotHitFraction = NotHitAngle / 90f;

                        float random = (float)Tools.Random.NextDouble();

                        if (HitAngle < DeflectionAngle && RicochetChance > random)
                        {
                            Tools.Debug($"Angle {HitAngle} < {DeflectionAngle}");
                            // Apply impulse
                            float impulse = p.Ammo.ProjectileHitImpulse * NotHitFraction * MaxVelocityTransfer;
                            if (hit.HitEntity.Physics != null)
                            {
                                hit.HitEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, p.Velocity * impulse * -hit.Normal, hit.Position, null);
                            }

                            // apply partial damage
                            float damage = p.Ammo.ProjectileMassDamage * NotHitFraction * MaxDamageTransfer;
							if (block != null && MyAPIGateway.Session.IsServer)
							{
                                Tools.Debug($"damage {damage}");
                                block.DoDamage(damage, MyStringHash.GetOrCompute(p.Ammo.SubtypeId), false, null, p.ShooterId);
							}

                            // reduce velocity
                            p.Velocity -= p.Velocity * NotHitFraction * MaxVelocityTransfer;

                            // reflect
                            p.Velocity = Vector3.Reflect(p.Velocity, hit.Normal);

                            // calculate new direction                 
                            p.Direction = Vector3D.Normalize(p.Velocity);
                            p.Position = hit.Position + (p.Direction * 0.5f);
                            p.Origin = p.Position;

                            //if (!MyAPIGateway.Utilities.IsDedicated)
                            //{
                            //    MatrixD world = MatrixD.CreateFromDir(hit.Normal);
                            //    world.Translation = hit.Position;

                            //    MyParticleEffect effect;
                            //    MyParticlesManager.TryCreateParticleEffect("Collision_Sparks_Directional", world, out effect);

                            //    effect.Loop = false;
                            //    effect.UserScale = 0.5f;
                            //    effect.UserEmitterScale = 16f;
                            //    effect.UserRadiusMultiplier = 0.1f;
                            //    effect.UserBirthMultiplier = 20f;
                            //    effect.DurationMin = 0.015f;
                            //    effect.DurationMax = 0.025f;
                            //    effect.SetRandomDuration();
                            //}
                        }
                        else
                        {
                            if (block != null && MyAPIGateway.Session.IsServer)
                            {
                                block.DoDamage(p.Ammo.ProjectileMassDamage, MyStringHash.GetOrCompute(p.Ammo.SubtypeId), true);
                            }

                            p.Expired = true;
                        }
                    }
                }
            }
        }
	}
}
