using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace WeaponsOverhaul
{
    public class DamageDefinition
    {
        public IMyDestroyableObject Victim;

        public float Damage;

        public MyStringHash DamageType;

        public bool Sync;

        public MyHitInfo? Hit;

        public long AttackerId;

        public IMyEntity ImpulseEntity;

        public Vector3 ImpulseForce;

        public Vector3 ImpulsePosition;
    }
}
