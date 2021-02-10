using ProtoBuf;
using Sandbox.Game.Entities;
using System.Xml.Serialization;
using static Sandbox.Definitions.MyWeaponDefinition;

namespace WeaponsOverhaul
{
	[ProtoContract]
	public class WeaponAmmoDefinition
	{
		[ProtoMember(10)]
		public int RateOfFire;

		[ProtoMember(20)]
		public int ShotsInBurst;

		[ProtoMember(30)]
		public string ShootSound;

		[XmlIgnore]
		public MySoundPair ShootSoundPair;

		public WeaponAmmoDefinition Clone()
		{
			return new WeaponAmmoDefinition {
				RateOfFire = RateOfFire,
				ShotsInBurst = ShotsInBurst,
				ShootSound = ShootSound,
				ShootSoundPair = ShootSoundPair
			};
		}

		public static WeaponAmmoDefinition CreateFromKeenDefinition(MyWeaponAmmoData a)
		{
			return new WeaponAmmoDefinition {
				RateOfFire = a.RateOfFire,
				ShotsInBurst = a.ShotsInBurst,

				ShootSound = a.ShootSound.SoundId.ToString(),
				ShootSoundPair = a.ShootSound,
			};
		}
	}
}
