using ProtoBuf;
using Sandbox.Definitions;
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
		public static Settings Static = new Settings();
		public static bool Initialized { get; private set; } = false;

		private const string IndexFilename = "Index";
		private const string AmmoFilename = "AmmoDefinitionExample";
		private const string WeaponFilename = "WeaponDefinitionExample";

		[ProtoMember(1)]
		private List<AmmoDefinition> AmmoDefinitions = new List<AmmoDefinition>();

		[ProtoMember(2)]
		private List<WeaponDefinition> WeaponDefinitions = new List<WeaponDefinition>();

		[XmlIgnore]
		private static List<AmmoDefinition> KeenAmmoDefinitions = new List<AmmoDefinition>();

		[XmlIgnore]
		private static List<WeaponDefinition> KeenWeaponDefinitions = new List<WeaponDefinition>();

		[XmlIgnore]
		public static Dictionary<string, AmmoDefinition> AmmoDefinitionLookup { get; private set; } = new Dictionary<string, AmmoDefinition>();

		[XmlIgnore]
		public static Dictionary<string, WeaponDefinition> WeaponDefinitionLookup { get; private set; } = new Dictionary<string, WeaponDefinition>();

		/// <summary>
		/// returns a clone of the matching ammo defintion
		/// </summary>
		/// <param name="id">SubtypeId</param>
		/// <returns></returns>
		public static AmmoDefinition GetAmmoDefinition(string id)
		{
			if (AmmoDefinitionLookup.ContainsKey(id))
			{
				return AmmoDefinitionLookup[id].Clone();
			}

			Tools.Debug($"Ammo definition was not found! This is bad!");

			return new AmmoDefinition();
		}

		/// <summary>
		/// returns a clone of the matching weapon defintion
		/// </summary>
		/// <param name="id">SubtypeId</param>
		/// <returns></returns>
		public static WeaponDefinition GetWeaponDefinition(string id)
		{
			if (WeaponDefinitionLookup.ContainsKey(id))
			{
				return WeaponDefinitionLookup[id].Clone();
			}

			Tools.Debug($"Weapon definition was not found! This is bad!");

			return new WeaponDefinition();

		}

		/// <summary>
		/// From server to client only
		/// This function is called when clients make a pull request
		/// </summary>
		/// <param name="s"></param>
		public static void SetUserDefinitions(Settings s)
		{
			Static.AmmoDefinitions = s.AmmoDefinitions;
			Static.WeaponDefinitions = s.WeaponDefinitions;
			BuildLookupDictionaries();
		}

		/// <summary>
		/// does a complete reload of definitions
		/// </summary>
		public static void Load()
		{
			LoadKeenDefinitions();
			LoadUserDefinitions();
			BuildLookupDictionaries();
			Initialized = true;
		}

		/// <summary>
		/// Clears the lookup dictionaries
		/// add the new definitions, first keens then user definitions replace
		/// more than definition with the same subtypeid will be replaced with the lates
		/// </summary>
		private static void BuildLookupDictionaries() 
		{
			AmmoDefinitionLookup.Clear();
			WeaponDefinitionLookup.Clear();

			foreach (AmmoDefinition a in KeenAmmoDefinitions)
			{
				if (AmmoDefinitionLookup.ContainsKey(a.SubtypeId))
				{
					AmmoDefinitionLookup[a.SubtypeId] = a;
				}
				else
				{
					AmmoDefinitionLookup.Add(a.SubtypeId, a);
				}
			}

			foreach (AmmoDefinition a in Static.AmmoDefinitions)
			{
				if (AmmoDefinitionLookup.ContainsKey(a.SubtypeId))
				{
					AmmoDefinitionLookup[a.SubtypeId] = a;
				}
				else
				{
					Tools.Error($"Could not find an existing defintion for {a.SubtypeId}");
				}
			}

			foreach (WeaponDefinition w in KeenWeaponDefinitions)
			{
				if (WeaponDefinitionLookup.ContainsKey(w.SubtypeId))
				{
					WeaponDefinitionLookup[w.SubtypeId] = w;
				}
				else
				{
					WeaponDefinitionLookup.Add(w.SubtypeId, w);
				}
			}

			foreach (WeaponDefinition w in Static.WeaponDefinitions)
			{
				if (WeaponDefinitionLookup.ContainsKey(w.SubtypeId))
				{
					WeaponDefinitionLookup[w.SubtypeId] = w;
				}
				else
				{
					Tools.Error($"Could not find an existing defintion for {w.SubtypeId}");
				}
			}
		}

		/// <summary>
		/// Loads user definitons for ammo and weapons
		/// </summary>
		private static void LoadUserDefinitions() 
		{
			Tools.Debug($"Loading User Definitions");
			Index FileIndex = null;
			try
			{
				TextReader r = MyAPIGateway.Utilities.ReadFileInWorldStorage(IndexFilename, typeof(Index));
				FileIndex = MyAPIGateway.Utilities.SerializeFromXML<Index>(r.ReadToEnd());

			}
			catch (FileNotFoundException fnfe)
			{
				Tools.Info($"The File Index did not exist. Loading defaults");
				FileIndex = new Index() {
					AmmoDefinitionFiles = new List<string> { AmmoFilename },
					WeaponDefinitionFiles = new List<string> { WeaponFilename },
				};

				TextWriter w;
				w = MyAPIGateway.Utilities.WriteFileInWorldStorage(IndexFilename, typeof(AmmoDefinition));
				w.Write(MyAPIGateway.Utilities.SerializeToXML(FileIndex));
				w.Close();

				try
				{
					w = MyAPIGateway.Utilities.WriteFileInWorldStorage(AmmoFilename, typeof(AmmoDefinition));
					w.Write(MyAPIGateway.Utilities.SerializeToXML(new AmmoDefinition {
						Enabled = false,
						SubtypeId = AmmoFilename,
						DesiredSpeed = 400,
						SpeedVariance = 0,
						MaxTrajectory = 1000,
						BackkickForce = 50,
						ProjectileTrailScale = 1.1f,
						ProjectileHitImpulse = 5,
						ProjectileMassDamage = 150,
						ProjectileHealthDamage = 33,
						ProjectileHeadShotDamage = 0,
					}));
					w.Close();

					w = MyAPIGateway.Utilities.WriteFileInWorldStorage(WeaponFilename, typeof(WeaponDefinition));
					w.Write(MyAPIGateway.Utilities.SerializeToXML(new WeaponDefinition {
						Enabled = false,
						SubtypeId = WeaponFilename,
						NoAmmoSound = "WepPlayRifleNoAmmo",
						SecondarySound = "WepShipGatlingRotation",
						DeviateShotAngle = 0.1f,
						ReleaseTimeAfterFire = 100,
						MuzzleFlashLifeSpan = 40,
						ReloadTime = 3000,
						AmmoData = new WeaponAmmoDefinition {
							ShootSound = "WepGatlingTurretShot",
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
						Static.AmmoDefinitions.Add(a);
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
						Static.WeaponDefinitions.Add(w);
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
			Tools.Debug($"Loading Keen Definitions");
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

						if (weaponDef.HasMissileAmmoDefined)
						{
							Tools.Debug($"Skipping keen weapon definition: {block.WeaponDefinitionId}");
							continue;
						}
						else
						{
							Tools.Debug($"Loading keen weapon definition: {block.WeaponDefinitionId}");
						}

						// stop vanilla projectile from firing
						// Thanks for the help Digi
						for (int i = 0; i < weaponDef.WeaponAmmoDatas.Length; i++)
						{
							var ammoData = weaponDef.WeaponAmmoDatas[i];

							if (ammoData == null)
								continue;

							ammoData.ShootIntervalInMiliseconds = int.MaxValue;
						}

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
