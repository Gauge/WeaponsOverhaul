using VRage.Game.ModAPI.Interfaces;
using VRage.Utils;
using VRageMath;

namespace WeaponsOverhaul
{
    public struct DamageDefinition
    {
        public long ShooterId;

        public IMyDestroyableObject Victim;

        public float Damage;

        public MyStringHash DamageType;
    }
}
