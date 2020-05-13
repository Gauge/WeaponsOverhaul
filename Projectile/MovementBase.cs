using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace WeaponsOverhaul
{
    //public abstract class ProjectileMovmentBase
    //{
    //    public abstract void Update(ref Projectile p);
    //}

    public class MovementBase
    {
        public static void Update(Projectile p)
        {
            AmmoDefinition ammo = Settings.AmmoDefinitionLookup[p.AmmoDefinitionId];

            p.LastPosition = p.Position;

            Vector3D travel = p.Velocity * Tools.Tick;
            p.Position += travel;
            p.DistanceTraveled += travel.LengthSquared();

            p.Lifetime++;
            p.HasExpired = p.DistanceTraveled * p.Lifetime > ammo.MaxTrajectory * ammo.MaxTrajectory;
        }
    }

}
