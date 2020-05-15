using ProtoBuf;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using VRage.Game;

namespace WeaponsOverhaul
{
	[ProtoContract]
	public class Settings
	{
		public static Action OnSettingsUpdated;

		public static Settings Static = new Settings();
		public static bool Initialized { get; private set; } = false;

		private const string SettingsFilename = "Setting.cfg";

		[ProtoMember(1)]
		public int Version;

		[ProtoMember(2)]
		public bool WriteDefaultDefinitionsToFile;

		[ProtoMember(10)]
		public bool DrawMuzzleFlash;

		[ProtoMember(30)]
		public List<string> Blacklist = new List<string>();

		[ProtoMember(31)]
		private List<AmmoDefinition> AmmoDefinitions = new List<AmmoDefinition>();

		[ProtoMember(32)]
		private List<WeaponDefinition> WeaponDefinitions = new List<WeaponDefinition>();

		private static List<AmmoDefinition> KeenAmmoDefinitions = new List<AmmoDefinition>();
		private static List<WeaponDefinition> KeenWeaponDefinitions = new List<WeaponDefinition>();

		public static Dictionary<string, AmmoDefinition> AmmoDefinitionLookup { get; private set; } = new Dictionary<string, AmmoDefinition>();
		public static Dictionary<string, WeaponDefinition> WeaponDefinitionLookup { get; private set; } = new Dictionary<string, WeaponDefinition>();

		// default values
		public Settings()
		{
			Version = 1;
			WriteDefaultDefinitionsToFile = true;
			DrawMuzzleFlash = true;
			Blacklist.Add("SubtypeId for blacklisted weapons go here. then restart.");
		}

		/// <summary>
		/// From server to client only
		/// This function is called when clients make a pull request
		/// </summary>
		public static void SetUserDefinitions(Settings s)
		{
			Static = s;
			BuildLookupDictionaries();
		}

		/// <summary>
		/// does a complete reload of definitions
		/// </summary>
		public static void Load()
		{
			if (FileExistsInWorldStorage(SettingsFilename, typeof(Settings)))
			{
				try
				{
					TextReader r = MyAPIGateway.Utilities.ReadFileInWorldStorage(SettingsFilename, typeof(Settings));
					Settings s = MyAPIGateway.Utilities.SerializeFromXML<Settings>(r.ReadToEnd());
					r.Close();

					Static = s;
					Tools.Info($"General settings loaded");
				}
				catch
				{
					Tools.Error($"Could not load settings from file \"{SettingsFilename}\". File is corrupted. Using Defaults");
					Static = new Settings();
				}
				
			}
			else
			{
				WriteDefaultSettings();
			}

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

			OnSettingsUpdated?.Invoke();
		}

		/// <summary>
		/// keens function is bugged and looks at local storage. this is a work around.
		/// </summary>
		private static bool FileExistsInWorldStorage(string filename, Type type)
		{
			try
			{
				TextReader r = MyAPIGateway.Utilities.ReadFileInWorldStorage(filename, type);
				r.Close();
				return true;
			}
			catch 
			{
				return false;
			}
		}

		/// <summary>
		/// Loads user definitons for ammo and weapons
		/// </summary>
		private static void LoadUserDefinitions() 
		{
			Type ammoType = typeof(AmmoDefinition);
			foreach (AmmoDefinition def in KeenAmmoDefinitions)
			{ 
				if (FileExistsInWorldStorage(def.SubtypeId, ammoType))
				{
					try
					{
						TextReader r = MyAPIGateway.Utilities.ReadFileInWorldStorage(def.SubtypeId, ammoType);
						AmmoDefinition a = MyAPIGateway.Utilities.SerializeFromXML<AmmoDefinition>(r.ReadToEnd());
						r.Close();

						Tools.Info($"{((a.Enabled) ? "(ENABLED)" : "(DISABLED)")} Loading user ammo definition {a.SubtypeId}");

						if (a.Enabled)
						{
							Static.AmmoDefinitions.Add(a);
						}
					}
					catch
					{
						Tools.Error($"Failed to load file {def.SubtypeId}");
					}
				}
			}

			Type weaponType = typeof(WeaponDefinition);
			foreach (WeaponDefinition def in KeenWeaponDefinitions)
			{
				if (FileExistsInWorldStorage(def.SubtypeId, weaponType))
				{
					try
					{
						TextReader r = MyAPIGateway.Utilities.ReadFileInWorldStorage(def.SubtypeId, weaponType);
						WeaponDefinition w = MyAPIGateway.Utilities.SerializeFromXML<WeaponDefinition>(r.ReadToEnd());
						r.Close();

						Tools.Info($"{((w.Enabled) ? "(ENABLED)" : "(DISABLED)")} Loading user weapon definition {w.SubtypeId}");

						if (w.Enabled)
						{
							Static.WeaponDefinitions.Add(w);
						}
					}
					catch
					{
						Tools.Error($"Failed to load file {def.SubtypeId}");
					}
				}
			}
		}

		/// <summary>
		/// This converts keens definitions into mostly identical copies that are used within the system.
		/// </summary>
		private static void LoadKeenDefinitions()
		{
			Type ammoType = typeof(AmmoDefinition);
			Type weaponType = typeof(WeaponDefinition);
			foreach (MyDefinitionBase def in MyDefinitionManager.Static.GetAllDefinitions())
			{
				try
				{
					if (def is MyAmmoMagazineDefinition)
					{
						MyAmmoDefinition ammo = MyDefinitionManager.Static.GetAmmoDefinition((def as MyAmmoMagazineDefinition).AmmoDefinitionId);

						if (ammo.IsExplosive)
						{
							Tools.Info($"Skipping keen ammo definition: {ammo.Id}");
							continue;
						}
						else
						{
							Tools.Info($"Loading keen ammo definition: {ammo.Id}");
						}

						AmmoDefinition a = AmmoDefinition.CreateFromKeenDefinition(ammo as MyProjectileAmmoDefinition);
						KeenAmmoDefinitions.Add(a);

						if (Static.WriteDefaultDefinitionsToFile && !FileExistsInWorldStorage(a.SubtypeId, ammoType))
						{
							try
							{
								AmmoDefinition ca = a.Clone();
								ca.Enabled = false;

								TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(ca.SubtypeId, ammoType);
								writer.Write(MyAPIGateway.Utilities.SerializeToXML(ca));
								writer.Close();

							}
							catch
							{
								Tools.Error($"Unable to write to file {a.SubtypeId}");
							}
						}

					}
					else if (def is MyWeaponBlockDefinition)
					{
						MyWeaponBlockDefinition block = def as MyWeaponBlockDefinition;
						MyWeaponDefinition weaponDef = MyDefinitionManager.Static.GetWeaponDefinition(block.WeaponDefinitionId);

						if (weaponDef.HasMissileAmmoDefined)
						{
							Tools.Info($"Skipping keen weapon definition: {block.WeaponDefinitionId}");
							continue;
						}
						else
						{
							Tools.Info($"Loading keen weapon definition: {block.WeaponDefinitionId}");
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

						if (Static.WriteDefaultDefinitionsToFile && !FileExistsInWorldStorage(w.SubtypeId, weaponType))
						{
							try
							{
								WeaponDefinition cw = w.Clone();
								cw.Enabled = false;

								TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(w.SubtypeId, weaponType);
								writer.Write(MyAPIGateway.Utilities.SerializeToXML(cw));
								writer.Close();

							}
							catch
							{
								Tools.Error($"Unable to write to file {w.SubtypeId}");
							}
						}
					}
				}
				catch (Exception e)
				{
					Tools.Error($"Failed to load definition: {def.Id}\n{e}");
				}
			}
		}

		/// <summary>
		/// Writes default settings to file
		/// </summary>
		private static void WriteDefaultSettings()
		{
			Tools.Info($"Saving default settings");
			try
			{
				Settings s = Static;
				s.AmmoDefinitions = new List<AmmoDefinition>();
				s.WeaponDefinitions = new List<WeaponDefinition>();

				TextWriter w = MyAPIGateway.Utilities.WriteFileInWorldStorage(SettingsFilename, typeof(Settings));
				w.Write(MyAPIGateway.Utilities.SerializeToXML(s));
				w.Close();

			}
			catch
			{
				Tools.Error("Failed to save default Settings");
			}
		}

	}
}
