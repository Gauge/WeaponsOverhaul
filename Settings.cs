using Microsoft.Xml.Serialization.GeneratedAssembly;
using ProtoBuf;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using VRage.Game;

namespace WeaponsOverhaul
{

	[ProtoContract]
	public class Index
	{
		[ProtoMember(1)]
		public List<string> AmmoDefinitionFiles = new List<string>();

		[ProtoMember(2)]
		public List<string> WeaponDefinitionFiles = new List<string>();
	}

	[ProtoContract]
	public class Settings
	{
		[XmlIgnore]
		private static List<AmmoDefinition> KeenAmmoDefinitions = new List<AmmoDefinition>();

		[XmlIgnore]
		private static List<WeaponDefinition> KeenWeaponDefinitions = new List<WeaponDefinition>();

		[ProtoMember(1)]
		private static List<AmmoDefinition> AmmoDefinitions = new List<AmmoDefinition>();

		[ProtoMember(2)]
		private static List<WeaponDefinition> WeaponDefinitions = new List<WeaponDefinition>();

		[XmlIgnore]
		public static Dictionary<string, AmmoDefinition> AmmoDefinitionLookup { get; private set; } = new Dictionary<string, AmmoDefinition>();

		[XmlIgnore]
		public static Dictionary<string, WeaponDefinition> WeaponDefinitionLookup { get; private set; } = new Dictionary<string, WeaponDefinition>();


		public static void Load()
		{
			LoadKeenDefinitions();
			LoadUserDefinitions();
			BuildLookupDictionaries();
		}

		private static void BuildLookupDictionaries() 
		{
			AmmoDefinitionLookup.Clear();
			WeaponDefinitionLookup.Clear();

			foreach (AmmoDefinition a in KeenAmmoDefinitions)
			{
				AmmoDefinitionLookup.Add(a.SubtypeId, a);
			}

			foreach (WeaponDefinition w in KeenWeaponDefinitions)
			{
				WeaponDefinitionLookup.Add(w.SubtypeId, w);
			}

			foreach (AmmoDefinition a in AmmoDefinitions)
			{
				if (AmmoDefinitionLookup.ContainsKey(a.SubtypeId))
				{
					AmmoDefinitionLookup[a.SubtypeId] = a;
				} 
			}

			foreach (WeaponDefinition w in WeaponDefinitions)
			{
				if (WeaponDefinitionLookup.ContainsKey(w.SubtypeId))
				{
					WeaponDefinitionLookup[w.SubtypeId] = w;
				}
			}
		}

		private static void LoadUserDefinitions() 
		{
			Index FileIndex = null;
			try
			{
				TextReader r = MyAPIGateway.Utilities.ReadFileInWorldStorage("Index", typeof(Index));
				FileIndex = MyAPIGateway.Utilities.SerializeFromXML<Index>(r.ReadToEnd());

			}
			catch (FileNotFoundException fnfe)
			{
				Tools.Info($"The File Index did not exist. Loading defaults");
				FileIndex = new Index() {
					AmmoDefinitionFiles = new List<string> { "AmmoDefinitionExample" },
					WeaponDefinitionFiles = new List<string> { "WeaponDefinitionExample" },
				};

				try
				{
					TextWriter w;
					w = MyAPIGateway.Utilities.WriteFileInWorldStorage("AmmoDefinitionExample", typeof(AmmoDefinition));
					w.Write(MyAPIGateway.Utilities.SerializeToXML(new AmmoDefinition {
						Enabled = false,
						SubtypeId = "AmmoDefinitionExample",
						DesiredSpeed = 400,
						SpeedVariance = 0,
						MaxTrajectory = 1000,
						BackkickForce = 50,
						PhysicalMaterial = "GunBullet",
						ProjectileTrailScale = 1.1f,
						ProjectileHitImpulse = 5,
						ProjectileMassDamage = 150,
						ProjectileHealthDamage = 33,
						ProjectileHeadShotDamage = 0,
					}));
					w.Close();

					w = MyAPIGateway.Utilities.WriteFileInWorldStorage("WeaponDefinitionExample", typeof(WeaponDefinition));
					w.Write(MyAPIGateway.Utilities.SerializeToXML(new WeaponDefinition {
						Enabled = false,
						SubtypeId = "WeaponDefinitionExample",
						NoAmmoSound = new MySoundPair("WepPlayRifleNoAmmo"),
						ReloadSound = new MySoundPair("ReloadSoundShouldGoHere"),
						SecondarySound = new MySoundPair("WepShipGatlingRotation"),
						DeviateShotAngle = 0.1f,
						ReleaseTimeAfterFire = 100,
						MuzzleFlashLifeSpan = 40,
						ReloadTime = 3000,
						AmmoData = new WeaponAmmoDefinition {
							ShootSound = new MySoundPair("WepGatlingTurretShot"),
							ShotsInBurst = 1,
							RateOfFire = 600,
						},
					}));
					w.Close();
				}
				catch (Exception ex)
				{
					Tools.Error($"Failed to create sample definitions:\n{ex}");
				}
			}
			catch (Exception e)
			{
				Tools.Info($"Failed to read the index file. the file is not formatted correctly:\n{e}");
			}

			if (FileIndex == null)
				return;

			foreach (string filename in FileIndex.AmmoDefinitionFiles)
			{
				try
				{
					TextReader r = MyAPIGateway.Utilities.ReadFileInWorldStorage(filename, typeof(AmmoDefinition));
					AmmoDefinition a = MyAPIGateway.Utilities.SerializeFromXML<AmmoDefinition>(r.ReadToEnd());

					Tools.Debug($"{((a.Enabled) ? "(ENABLED)" : "(DISABLED)")} Loading user ammo definition {a.SubtypeId}");

					if (a.Enabled)
					{
						AmmoDefinitions.Add(a);
					}
				}
				catch (Exception e)
				{
					Tools.Error($"Failed to load Ammo Definition from file. The filename could be misspelled in the Index file. The file could be missing or the file could be incorrectly formatted.\n{e}");
				}
			}

			foreach (string filename in FileIndex.WeaponDefinitionFiles)
			{
				try
				{
					TextReader r = MyAPIGateway.Utilities.ReadFileInWorldStorage(filename, typeof(WeaponDefinition));
					WeaponDefinition w = MyAPIGateway.Utilities.SerializeFromXML<WeaponDefinition>(r.ReadToEnd());

					Tools.Debug($"{((w.Enabled) ? "(ENABLED)" : "(DISABLED)")} Loading user weapon definition {w.SubtypeId}");

					if (w.Enabled)
					{
						WeaponDefinitions.Add(w);
					}
				}
				catch (Exception e)
				{
					Tools.Error($"Failed to load Ammo Definition from file. The filename could be misspelled in the Index file. The file could be missing or the file could be incorrectly formatted.\n{e}");
				}
			}
		}

		/// <summary>
		/// This converts keens definitions into mostly identical copies that are used within the system.
		/// </summary>
		private static void LoadKeenDefinitions()
		{
			foreach (MyDefinitionBase def in MyDefinitionManager.Static.GetAllDefinitions())
			{
				try
				{
					if (def is MyAmmoMagazineDefinition)
					{
						MyAmmoDefinition ammo = MyDefinitionManager.Static.GetAmmoDefinition((def as MyAmmoMagazineDefinition).AmmoDefinitionId);
						if (ammo.IsExplosive)
							continue;

						Tools.Debug($"Loading keen ammo definition: {ammo.Id.SubtypeId}");

						AmmoDefinition a = AmmoDefinition.CreateFromKeenDefinition(ammo as MyProjectileAmmoDefinition);
						KeenAmmoDefinitions.Add(a);
					}
					else if (def is MyWeaponBlockDefinition)
					{

						MyWeaponBlockDefinition block = def as MyWeaponBlockDefinition;
						Tools.Debug($"Loading keen weapon definition: {block.WeaponDefinitionId.SubtypeId}");
						MyWeaponDefinition weaponDef = MyDefinitionManager.Static.GetWeaponDefinition(block.WeaponDefinitionId);
						WeaponDefinition w = WeaponDefinition.CreateFromKeenDefinition(weaponDef);
						KeenWeaponDefinitions.Add(w);
					}
				}
				catch (Exception e)
				{
					Tools.Error($"Failed to load definition: {def.Id}\n{e}");
				}
			}
		}
	}
}
