using ProtoBuf;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Xml.Serialization;

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

		[ProtoMember(100)]
		public bool AiEnabled;
		[ProtoMember(110)]
		public int MinElevationDegrees;
		[ProtoMember(120)]
		public int MaxElevationDegrees;
		[ProtoMember(130)]
		public int MinAzimuthDegrees;
		[ProtoMember(140)]
		public int MaxAzimuthDegrees;
		[ProtoMember(150)]
		public bool IdleRotation;
		[ProtoMember(160)]
		public float MaxRangeMeters;
		[ProtoMember(170)]
		public float RotationSpeed;
		[ProtoMember(180)]
		public float ElevationSpeed;
		[ProtoMember(190)]
		public float MinFov;
		[ProtoMember(200)]
		public float MaxFov;
		[ProtoMember(210)]
		public int AmmoPullAmount;
		[ProtoMember(220)]
		public float InventoryFillFactorMin;

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

				AiEnabled = AiEnabled,
				MinElevationDegrees = MinElevationDegrees,
				MaxElevationDegrees = MaxElevationDegrees,
				MinAzimuthDegrees = MinAzimuthDegrees,
				MaxAzimuthDegrees = MaxAzimuthDegrees,
				IdleRotation = IdleRotation,
				MaxRangeMeters = MaxRangeMeters,
				RotationSpeed = RotationSpeed,
				ElevationSpeed = ElevationSpeed,
				MinFov = MinFov,
				MaxFov = MaxFov,
				AmmoPullAmount = AmmoPullAmount,
				InventoryFillFactorMin = InventoryFillFactorMin,
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

			AiEnabled = w.AiEnabled;
			MinElevationDegrees = w.MinElevationDegrees;
			MaxElevationDegrees = w.MaxElevationDegrees;
			MinAzimuthDegrees = w.MinAzimuthDegrees;
			MaxAzimuthDegrees = w.MaxAzimuthDegrees;
			IdleRotation = w.IdleRotation;
			MaxRangeMeters = w.MaxRangeMeters;
			RotationSpeed = w.RotationSpeed;
			ElevationSpeed = w.ElevationSpeed;
			MinFov = w.MinFov;
			MaxFov = w.MaxFov;
			AmmoPullAmount = w.AmmoPullAmount;
			InventoryFillFactorMin = w.InventoryFillFactorMin;
		}

		public static WeaponDefinition CreateFromKeenDefinition(MyWeaponBlockDefinition b, MyWeaponDefinition w)
		{
			if (b is MyLargeTurretBaseDefinition)
			{
				MyLargeTurretBaseDefinition lb = b as MyLargeTurretBaseDefinition;
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

					AiEnabled = lb.AiEnabled,
					MinElevationDegrees = lb.MinElevationDegrees,
					MaxElevationDegrees = lb.MaxElevationDegrees,
					MinAzimuthDegrees = lb.MinAzimuthDegrees,
					MaxAzimuthDegrees = lb.MaxAzimuthDegrees,
					IdleRotation = lb.IdleRotation,
					MaxRangeMeters = lb.MaxRangeMeters,
					RotationSpeed = lb.RotationSpeed,
					ElevationSpeed = lb.ElevationSpeed,
					MinFov = lb.MinFov,
					MaxFov = lb.MaxFov,
					AmmoPullAmount = lb.AmmoPullAmount,
					InventoryFillFactorMin = lb.InventoryFillFactorMin,
				};
			}
			else
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
					InventoryFillFactorMin = b.InventoryFillFactorMin,
				};
			}
		}

		public override string ToString()
		{
			return MyAPIGateway.Utilities.SerializeToXML(this);
		}

	}
}
