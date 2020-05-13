using VRage.Game.ModAPI.Interfaces;
using VRage.Utils;

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
