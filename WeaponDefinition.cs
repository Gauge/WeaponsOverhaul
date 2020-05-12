using ProtoBuf;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
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
		[ProtoMember(50)]
		public float ReleaseTimeAfterFire;

		//[XmlIgnore]
		//public MyStringHash PhysicalMaterial;
		[ProtoMember(60)]
		public int MuzzleFlashLifeSpan;

		[ProtoMember(61)]
		public string MuzzleFlashSpriteName;

		[ProtoMember(70)]
		public string NoAmmoSound;
		[ProtoMember(80)]
		public string ReloadSound;
		[ProtoMember(90)]
		public string SecondarySound;

		[XmlIgnore]
		public MySoundPair NoAmmoSoundPair;
		[XmlIgnore]
		public MySoundPair ReloadSoundPair;
		[XmlIgnore]
		public MySoundPair SecondarySoundPair;

		public WeaponDefinition Clone()
		{
			return new WeaponDefinition {
				Enabled = Enabled,
				SubtypeId = SubtypeId,
				DeviateShotAngle = DeviateShotAngle,
				ReloadTime = ReloadTime,
				AmmoData = AmmoData,
				ReleaseTimeAfterFire = ReleaseTimeAfterFire,
				//PhysicalMaterial = PhysicalMaterial,
				MuzzleFlashLifeSpan = MuzzleFlashLifeSpan,
				MuzzleFlashSpriteName = MuzzleFlashSpriteName,
				NoAmmoSound = NoAmmoSound,
				ReloadSound = ReloadSound,
				SecondarySound = SecondarySound,
				NoAmmoSoundPair = NoAmmoSoundPair,
				ReloadSoundPair = ReloadSoundPair,
				SecondarySoundPair = SecondarySoundPair,
			};
		}

		public void Copy(WeaponDefinition w)
		{
			Enabled = w.Enabled;
			SubtypeId = w.SubtypeId;
			DeviateShotAngle = w.DeviateShotAngle;
			ReloadTime = w.ReloadTime;
			AmmoData = w.AmmoData;
			ReleaseTimeAfterFire = w.ReleaseTimeAfterFire;
			//PhysicalMaterial = w.PhysicalMaterial;
			MuzzleFlashLifeSpan = w.MuzzleFlashLifeSpan;
			MuzzleFlashSpriteName = w.MuzzleFlashSpriteName;
			NoAmmoSound = w.NoAmmoSound;
			ReloadSound = w.ReloadSound;
			SecondarySound = w.SecondarySound;
			NoAmmoSoundPair = w.NoAmmoSoundPair;
			ReloadSoundPair = w.ReloadSoundPair;
			SecondarySoundPair = w.SecondarySoundPair;
		}

		public static WeaponDefinition CreateFromKeenDefinition(MyWeaponDefinition w)
		{
			return new WeaponDefinition {
				Enabled = true,
				SubtypeId = w.Id.SubtypeId.String,
				DeviateShotAngle = w.DeviateShotAngle,
				ReloadTime = w.ReloadTime,
				ReleaseTimeAfterFire = w.ReleaseTimeAfterFire,
				//PhysicalMaterial = w.PhysicalMaterial,
				MuzzleFlashLifeSpan = w.MuzzleFlashLifeSpan,
				MuzzleFlashSpriteName = "Muzzle_Flash_Large",
				NoAmmoSound = w.NoAmmoSound.SoundId.ToString(),
				ReloadSound = w.ReloadSound.SoundId.ToString(),
				SecondarySound = w.SecondarySound.SoundId.ToString(),
				NoAmmoSoundPair = w.NoAmmoSound,
				ReloadSoundPair = w.ReloadSound,
				SecondarySoundPair = w.SecondarySound,
				AmmoData = WeaponAmmoDefinition.CreateFromKeenDefinition(w.WeaponAmmoDatas[0]),
			};
		}

		public override string ToString()
		{
			return MyAPIGateway.Utilities.SerializeToXML(this);
		}

	}

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
