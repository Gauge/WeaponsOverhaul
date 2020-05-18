using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SENetworkAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;
using WeaponsOverhaul.Definitions;

namespace WeaponsOverhaul
{
	[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
	public class Core : MyNetworkSessionComponent
	{

		#region setup
		public static ConcurrentQueue<DamageDefinition> DamageRequests = new ConcurrentQueue<DamageDefinition>();
		public static ConcurrentQueue<PhysicsDefinition> PhysicsRequests = new ConcurrentQueue<PhysicsDefinition>();
		private static HashSet<Projectile> PendingProjectiles = new HashSet<Projectile>();
		private static HashSet<Projectile> ActiveProjectiles = new HashSet<Projectile>();
		private static HashSet<Projectile> ExpiredProjectiles = new HashSet<Projectile>();

		public const ushort ModId = 12144;
		public const string ModName = "WeaponsOverhaul";
		public const string ModKeyword = "/weap";

		private NetSync<Settings> NetSettings;


		public static long ControlledGridId;
		private static bool DisplayNotification;
		private long LastNotification;
		//public static SerializableDefinitionId SelectedDefinition;
		private IMyShipController ActiveShipController;

		private List<WeaponControlLayer> GridWeapons = new List<WeaponControlLayer>();

		public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
		{
			NetworkAPI.LogNetworkTraffic = false;
			Tools.DebugMode = true;

			if (!NetworkAPI.IsInitialized)
			{
				NetworkAPI.Init(ModId, ModName, ModKeyword);
			}

			NetSettings = new NetSync<Settings>(this, TransferType.ServerToClient, Settings.Static);
			NetSettings.ValueChangedByNetwork += UpdateSettings;

			if (!MyAPIGateway.Session.IsServer)
			{
				Network.RegisterChatCommand("load", (args) => Network.SendCommand("load"));
			}
			else
			{
				Network.RegisterNetworkCommand("load", ServerCallback_Load);

				Network.RegisterChatCommand("load", (args) => {
					Settings.Load();
					NetSettings.Value = Settings.Static;
				});
			}

			Settings.Load();
		}


		public override void BeforeStart()
		{
			MyAPIGateway.Session.LocalHumanPlayer.Controller.ControlledEntityChanged += Changed;
			Changed(null, MyAPIGateway.Session.LocalHumanPlayer.Controller.ControlledEntity);
		}

		private void Changed(VRage.Game.ModAPI.Interfaces.IMyControllableEntity o, VRage.Game.ModAPI.Interfaces.IMyControllableEntity n)
		{
			GridWeapons.Clear();
			ControlledGridId = 0;

			ActiveShipController = n?.Entity as IMyShipController;
			//SelectedDefinition = Tools.GetSelectedHotbarDefinition(ActiveShipController);
			MyCubeGrid grid = (n?.Entity as MyCubeBlock)?.CubeGrid; // is controlling a turret

			if (grid != null)
			{
				ControlledGridId = grid.EntityId;
				foreach (MyCubeBlock block in grid.GetFatBlocks())
				{
					WeaponControlLayer layer = block.GameLogic.GetAs<WeaponControlLayer>();

					if (layer != null)
					{
						GridWeapons.Add(layer);
					}
				}
			}
		}

		private void Notify()
		{
			long current = DateTime.UtcNow.Ticks;
			if ((current - LastNotification) / TimeSpan.TicksPerMillisecond < 3000)
				return;
			
			int reloading = 0;
			int outOfAmmo = 0;
			int nonFunctional = 0;
			int off = 0;
			LastNotification = current;

			foreach (WeaponControlLayer layer in GridWeapons)
			{
				if (layer == null)
				{
					nonFunctional++;
				}
				else
				{
					IMyFunctionalBlock f = (layer.Entity as IMyFunctionalBlock);
					if (f == null)
					{
						nonFunctional++;
						continue;
					}

					if (!f.IsFunctional)
						nonFunctional++;
					if (!f.IsWorking)
						off++;
					if (layer.Weapon.IsOutOfAmmo)
						outOfAmmo++;
					if (layer.Weapon.IsReloading)
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
		}

		protected override void UnloadData()
		{
			Network.Close();
		}
		#endregion

		#region projectile logic

		public static void SpawnProjectile(Projectile data)
		{
			lock (PendingProjectiles)
			{
				PendingProjectiles.Add(data);
			}
		}

		public static void ExpireProjectile(Projectile data)
		{
			data.Expired = true;
			ExpiredProjectiles.Add(data);
		}

		public override void UpdateBeforeSimulation()
		{
			if (Tools.DebugMode && !MyAPIGateway.Utilities.IsDedicated)
			{
				MyAPIGateway.Utilities.ShowNotification($"Total Projectiles: {ActiveProjectiles.Count}, Pending: {PendingProjectiles.Count}, Expired: {ExpiredProjectiles.Count}", 1);
			}

			if (DisplayNotification)
			{
				Notify();
			}

			ActiveProjectiles.ExceptWith(ExpiredProjectiles);
			ExpiredProjectiles.Clear();

			ActiveProjectiles.UnionWith(PendingProjectiles);
			PendingProjectiles.Clear();

			MyAPIGateway.Parallel.ForEach(ActiveProjectiles, (Projectile p) => {
				p.Update();
			});

			DamageDefinition def;
			while (DamageRequests.TryDequeue(out def))
			{
				def.Victim?.DoDamage(def.Damage, def.DamageType, false, default(MyHitInfo), def.ShooterId);
			}

			PhysicsDefinition pDef;
			while (PhysicsRequests.TryDequeue(out pDef))
			{
				pDef.Target?.Physics?.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, pDef.Force, pDef.Position, Vector3.Zero);
			}
		}

		public override void Draw()
		{
			foreach (Projectile p in ActiveProjectiles)
			{
				p.Draw();
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

