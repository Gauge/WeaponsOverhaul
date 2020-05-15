using Sandbox.ModAPI;
using SENetworkAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace WeaponsOverhaul
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Core : MyNetworkSessionComponent
    {
		#region setup
		public static ConcurrentQueue<DamageDefinition> DamageRequests = new ConcurrentQueue<DamageDefinition>();
        private static HashSet<Projectile> PendingProjectiles = new HashSet<Projectile>();
        private static HashSet<Projectile> ActiveProjectiles = new HashSet<Projectile>();
        private static HashSet<Projectile> ExpiredProjectiles = new HashSet<Projectile>();

        public const ushort ModId = 12144;
        public const string ModName = "WeaponsOverhaul";
        public const string ModKeyword = "/weap";

        private NetSync<Settings> NetSettings;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            NetworkAPI.LogNetworkTraffic = false;
            Tools.DebugMode = false;

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
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                MyAPIGateway.Utilities.ShowNotification($"Total Projectiles: {ActiveProjectiles.Count}, Pending: {PendingProjectiles.Count}, Expired: {ExpiredProjectiles.Count}", 1);
            }

            ActiveProjectiles.ExceptWith(ExpiredProjectiles);
            ExpiredProjectiles.Clear();

            ActiveProjectiles.UnionWith(PendingProjectiles);
            PendingProjectiles.Clear();

            MyAPIGateway.Parallel.ForEach(ActiveProjectiles, (Projectile p) => {      
                p.Update();
            });

            DamageDefinition def;
            while (DamageRequests.Count > 0)
            {
                DamageRequests.TryDequeue(out def);
                def.Victim?.DoDamage(def.Damage, def.DamageType, false, default(MyHitInfo), def.ShooterId);
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

