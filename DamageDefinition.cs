using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace WeaponsOverhaul
{
    public class DamageDefinition
    {
        public long AttackerId;

        public IMyDestroyableObject Victim;

        public float Damage;

        public MyStringHash DamageType;

        public IMyEntity ImpulseEntity;

        public Vector3 ImpulseForce;

        public Vector3 ImpulsePosition;
    }
}
