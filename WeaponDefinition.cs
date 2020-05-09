using ProtoBuf;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using System.Xml.Serialization;
using VRage.Utils;
using static Sandbox.Definitions.MyWeaponDefinition;

namespace WeaponsOverhaul
{
	[ProtoContract]
	public class WeaponDefinition
	{
		[ProtoMember(1)]
		public bool Enabled;

		[ProtoMember(10)]
		public string SubtypeId;

		[ProtoMember(20)]
		public float DeviateShotAngle;

		[ProtoMember(30)]
		public int ReloadTime;

		[ProtoMember(40)]
		public WeaponAmmoDefinition AmmoData;

		//[ProtoMember(40)]
		//public List<WeaponAmmoDefinition> AmmoDatas = new List<WeaponAmmoDefinition>();

		// mod effects
		//[ProtoMember]
		//public Ramping Ramping;

		// Is not part of the config
		//[XmlIgnore]
		//public Randomizer Randomizer = new Randomizer();

		// Unused stuff
		[XmlIgnore]
		public float ReleaseTimeAfterFire;

		[XmlIgnore]
		public MyStringHash PhysicalMaterial;

		//[XmlIgnore]
		//public float DamageMultiplier;

		[XmlIgnore]
		public int MuzzleFlashLifeSpan;

		//[XmlIgnore]
		//public bool UseDefaultMuzzleFlash;

		[XmlIgnore]
		public MySoundPair NoAmmoSound;

		[XmlIgnore]
		public MySoundPair ReloadSound;

		[XmlIgnore]
		public MySoundPair SecondarySound;

		public WeaponDefinition Clone()
		{
			return new WeaponDefinition();
		}

		public static WeaponDefinition CreateFromKeenDefinition(MyWeaponDefinition w)
		{
			return new WeaponDefinition {
				Enabled = true,
				SubtypeId = w.Id.SubtypeId.String,
				DeviateShotAngle = w.DeviateShotAngle,
				ReloadTime = w.ReloadTime,
				ReleaseTimeAfterFire = w.ReleaseTimeAfterFire,
				DamageMultiplier = w.DamageMultiplier,
				PhysicalMaterial = w.PhysicalMaterial,
				MuzzleFlashLifeSpan = w.MuzzleFlashLifeSpan,
				NoAmmoSound = w.NoAmmoSound,
				ReloadSound = w.ReloadSound,
				SecondarySound = w.SecondarySound,
				AmmoData = WeaponAmmoDefinition.CreateFromKeenDefinition(w.WeaponAmmoDatas[0]),



			};
		}
	}

	[ProtoContract]
	public class WeaponAmmoDefinition
	{
		[ProtoMember(10)]
		public int RateOfFire;

		[ProtoMember(20)]
		public int ShotsInBurst;

		[XmlIgnore]
		public MySoundPair ShootSound;

		public WeaponAmmoDefinition Clone()
		{
			return new WeaponAmmoDefinition {
				RateOfFire = RateOfFire,
				ShotsInBurst = ShotsInBurst,
				ShootSound = ShootSound,
			};
		}

		public static WeaponAmmoDefinition CreateFromKeenDefinition(MyWeaponAmmoData a)
		{
			return new WeaponAmmoDefinition {
				RateOfFire = a.RateOfFire,
				ShotsInBurst = a.ShotsInBurst,
				ShootSound = a.ShootSound,
			};
		}
	}
}
