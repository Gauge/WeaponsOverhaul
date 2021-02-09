using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SENetworkAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.ObjectBuilders;
using VRageMath;

namespace WeaponsOverhaul
{
	[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
	public class Core : MySessionComponentBase
	{

		#region setup
		public const ushort ModId = 12144;
		public const string ModName = "WeaponsOverhaul";
		public const string ModKeyword = "/weap";

		private NetSync<Settings> NetSettings;

		private static bool DisplayNotification;
		private long LastNotification;
		private bool IsNotificationInitialized;

		public static long ControlledGridId;
		private static IMyShipController ActiveShipController;
		private static IMyLargeTurretBase ActiveTurret;
		private static SerializableDefinitionId SelectedDefinitionId;
		private static List<WeaponBase> GridWeapons = new List<WeaponBase>();

		public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
		{
			NetworkAPI.LogNetworkTraffic = true;
			Tools.DebugMode = true;

			IsNotificationInitialized = MyAPIGateway.Utilities.IsDedicated;

			if (!NetworkAPI.IsInitialized)
			{
				NetworkAPI.Init(ModId, ModName, ModKeyword);
			}

			NetSettings = new NetSync<Settings>(this, TransferType.ServerToClient, Settings.Static);
			NetSettings.ValueChangedByNetwork += UpdateSettings;
			//NetSettings.BeforeFetchRequestResponse += SettingsRequest;

			if (!MyAPIGateway.Session.IsServer)
			{
				NetworkAPI.Instance.RegisterChatCommand("load", (args) => NetworkAPI.Instance.SendCommand("load"));
			}
			else
			{
				NetworkAPI.Instance.RegisterNetworkCommand("load", ServerCallback_Load);

				NetworkAPI.Instance.RegisterChatCommand("load", (args) => {
					Settings.Load();
					NetSettings.Value = Settings.Static;
				});
			}

			Settings.Load();
			NetSettings.Value = Settings.Static;
		}

		//private void SettingsRequest(ulong steamid)
		//{
		//	NetSettings.SetValue(Settings.Static);

		//	Tools.Debug(MyAPIGateway.Utilities.SerializeToXML(Settings.Static));

		//}

		private void Changed(VRage.Game.ModAPI.Interfaces.IMyControllableEntity o, VRage.Game.ModAPI.Interfaces.IMyControllableEntity n)
		{

			foreach (WeaponBase w in GridWeapons)
			{
				w.State.Value &= ~WeaponState.ManualShoot;
			}

			GridWeapons.Clear();
			ControlledGridId = 0;

			ActiveTurret = n?.Entity as IMyLargeTurretBase;

			if (ActiveTurret == null)
			{
				ActiveShipController = n?.Entity as IMyShipController;
				SelectedDefinitionId = Tools.GetSelectedHotbarDefinition(ActiveShipController);
			}

			MyCubeGrid grid = (n?.Entity as MyCubeBlock)?.CubeGrid;

			if (grid != null)
			{
				ControlledGridId = grid.EntityId;
				foreach (MyCubeBlock block in grid.GetFatBlocks())
				{
					WeaponControlLayer layer = block.GameLogic.GetAs<WeaponControlLayer>();

					if (layer != null)
					{
						GridWeapons.Add(layer.Weapon);
					}
				}
			}
		}

		private void Notify()
		{
			long current = DateTime.UtcNow.Ticks;
			if ((current - LastNotification) / TimeSpan.TicksPerMillisecond < 3016)
				return;

			int reloading = 0;
			int outOfAmmo = 0;
			int nonFunctional = 0;
			int off = 0;
			LastNotification = current;

			foreach (WeaponBase w in GridWeapons)
			{
				if (w == null)
				{
					nonFunctional++;
				}
				else
				{
					if (w.Block == null)
					{
						nonFunctional++;
						continue;
					}

					if (!w.Block.IsFunctional)
						nonFunctional++;
					if (!w.Block.IsWorking)
						off++;
					if (w.IsOutOfAmmo)
						outOfAmmo++;
					if (w.IsReloading)
						reloading++;
				}
			}

			MyAPIGateway.Utilities.ShowNotification($"Weapons - off ({off}) damaged ({nonFunctional}) reloading ({reloading}) - out ({outOfAmmo})", 3000, "Red");
			DisplayNotification = false;
		}

		public static void NotifyNextFrame(long gridId)
		{
			if (ControlledGridId == gridId)
			{
				DisplayNotification = true;
			}
		}

		public void UpdateSettings(Settings o, Settings n, ulong steamId)
		{
			Settings.SetUserDefinitions(n);

			Tools.Debug(MyAPIGateway.Utilities.SerializeToXML(n));
		}

		public void HandleInputs()
		{
			if (MyAPIGateway.Gui.IsCursorVisible)
				return;

			if (ActiveShipController != null)
			{
				List<MyKeys> keys = new List<MyKeys>();
				MyAPIGateway.Input.GetPressedKeys(keys);

				foreach (var key in keys)
				{
					if (key == MyKeys.D1 ||
						key == MyKeys.D2 ||
						key == MyKeys.D3 ||
						key == MyKeys.D4 ||
						key == MyKeys.D5 ||
						key == MyKeys.D6 ||
						key == MyKeys.D7 ||
						key == MyKeys.D8 ||
						key == MyKeys.D9)
					{
						SelectedDefinitionId = Tools.GetSelectedHotbarDefinition(ActiveShipController);
					}
				}
			}

			if (MyAPIGateway.Input.IsNewPrimaryButtonPressed())
			{
				Tools.Debug("primary key pressed");
				if (ActiveTurret != null)
				{
					WeaponControlLayer layer = ActiveTurret.GameLogic.GetAs<WeaponControlLayer>();
					layer.Weapon.State.Value |= WeaponState.ManualShoot;
				}
				else
				{


					Tools.Debug($"looping over {GridWeapons.Count} weapons");
					foreach (WeaponBase w in GridWeapons)
					{
						if (SelectedDefinitionId == w.WeaponDefinition)
						{
							w.State.Value |= WeaponState.ManualShoot;
						}
					}
				}
			}

			if (MyAPIGateway.Input.IsNewPrimaryButtonReleased())
			{
				Tools.Debug("primary key released");
				if (ActiveTurret != null)
				{
					WeaponControlLayer layer = ActiveTurret.GameLogic.GetAs<WeaponControlLayer>();
					layer.Weapon.State.Value &= ~WeaponState.ManualShoot;
				}
				else
				{
					foreach (WeaponBase w in GridWeapons)
					{
						if (SelectedDefinitionId == w.WeaponDefinition)
						{
							w.State.Value &= ~WeaponState.ManualShoot;
						}
					}
				}
			}
		}

		public override void LoadData()
		{
			Static = this;
		}

		protected override void UnloadData()
		{
			Static = null;
		}
		#endregion

		#region projectile logic

		public static Core Static;

		private Projectile[] Projectiles = new Projectile[2048];
		private int projectileCount;
		private object projectileCreationLock = new object();

		public void Spawn(Vector3D origin, Vector3 direction, Vector3D startVelocity, long shooterId, AmmoDefinition ammo) 
		{
			lock (projectileCreationLock)
			{
				projectileCount++;
				if (Projectiles.Length < projectileCount)
				{
					Projectile[] newArray = new Projectile[Projectiles.Length * 2];
					Array.Copy(Projectiles, newArray, Projectiles.Length);
					Projectiles = newArray;
				}

				Projectiles[projectileCount - 1] = new Projectile(origin, direction, startVelocity, ammo, shooterId);
			}	
		}

		public override void UpdateBeforeSimulation()
		{
			if (Tools.DebugMode && !MyAPIGateway.Utilities.IsDedicated)
			{
				MyAPIGateway.Utilities.ShowNotification($"Total Projectiles: {projectileCount}", 1);
			}

			if (!IsNotificationInitialized && MyAPIGateway.Session?.LocalHumanPlayer != null)
			{
				Tools.Debug($"Controller: {MyAPIGateway.Session?.LocalHumanPlayer?.Controller != null}");
				Tools.Debug($"Entity: {MyAPIGateway.Session?.LocalHumanPlayer?.Controller?.ControlledEntity != null}");

				MyAPIGateway.Session.LocalHumanPlayer.Controller.ControlledEntityChanged += Changed;
				Changed(null, MyAPIGateway.Session.LocalHumanPlayer.Controller.ControlledEntity);
				IsNotificationInitialized = true;
			}

			if (DisplayNotification)
			{
				Notify();
			}

			for (int i = 0; i < projectileCount; i++)
			{
				Projectiles[i].Update();
				if (Projectiles[i].Expired)
				{
					int newIndex = projectileCount - 1;
					if (newIndex != i)
					{
						Projectiles[i] = Projectiles[newIndex];

						Projectiles[newIndex] = null;
					}		
					i--;
					projectileCount--;
				}
			}

			HandleInputs();
		}

		public override void Draw()
		{
			for (int i = 0; i < projectileCount; i++)
			{
				Projectiles[i].Draw();
			}
		}

		#endregion

		private void ServerCallback_Load(ulong steamId, string commandString, byte[] data, DateTime timestamp)
		{
			if (IsAllowedSpecialOperations(steamId))
			{
				Settings.Load();
				NetSettings.Value = Settings.Static;
			}
		}

		public static bool IsAllowedSpecialOperations(ulong steamId)
		{
			if (MyAPIGateway.Multiplayer.IsServer)
				return true;
			return IsAllowedSpecialOperations(MyAPIGateway.Session.GetUserPromoteLevel(steamId));
		}

		public static bool IsAllowedSpecialOperations(MyPromoteLevel level)
		{
			return level == MyPromoteLevel.SpaceMaster || level == MyPromoteLevel.Admin || level == MyPromoteLevel.Owner;
		}
	}
}

