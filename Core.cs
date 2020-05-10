using Sandbox.ModAPI;
using SENetworkAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;

namespace WeaponsOverhaul
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation | MyUpdateOrder.BeforeSimulation)]
    public class Core : MyNetworkSessionComponent
    {
        public static ConcurrentQueue<DamageDefinition> DamageRequests = new ConcurrentQueue<DamageDefinition>();
        private static HashSet<Projectile> PendingProjectiles = new HashSet<Projectile>();
        private static HashSet<Projectile> ActiveProjectiles = new HashSet<Projectile>();
        private static HashSet<Projectile> ExpiredProjectiles = new HashSet<Projectile>();


        public const ushort ModId = 12144;
        public const string ModName = "WeaponsOverhaul";
        public const string ModKeyword = "wo";

        public static Action OnSettingsUpdated;

        public NetSync<Settings> NetSettings;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            NetworkAPI.LogNetworkTraffic = true;
            if (!NetworkAPI.IsInitialized)
            {
                NetworkAPI.Init(ModId, ModName, ModKeyword);
            }

            Settings.Load();
            OnSettingsUpdated?.Invoke();

            NetSettings = new NetSync<Settings>(this, TransferType.ServerToClient, Settings.Static);
            NetSettings.ValueChangedByNetwork += UpdateSettings;
        }

        public void UpdateSettings(Settings old, Settings nu, ulong steamId)
        {
            Settings.SetUserDefinitions(nu);
            OnSettingsUpdated?.Invoke();
        }

        protected override void UnloadData()
        {
            Network.Close();
        }

        public static void SpawnProjectile(Projectile data)
        {
            lock (PendingProjectiles)
            {
                PendingProjectiles.Add(data);
            }
        }

        public override void UpdateBeforeSimulation()
        {
            ActiveProjectiles.ExceptWith(ExpiredProjectiles);
            ExpiredProjectiles.Clear();

            ActiveProjectiles.UnionWith(PendingProjectiles);
            PendingProjectiles.Clear();

            MyAPIGateway.Parallel.ForEach(ActiveProjectiles, (Projectile p) => {
                if (!p.Initialized)
                {
                    p.Init();
                }

                p.PreUpdate();

                if (p.DoCollisionCheck())
                {
                    p.PreCollitionDetection();
                    p.CollisionDetection();
                }

                p.Update();

                if (p.HasExpired)
                {
                    lock (ExpiredProjectiles)
                    {
                        ExpiredProjectiles.Add(p);
                    }
                }
            });

            DamageDefinition def;
            while (DamageRequests.Count > 0)
            {
                DamageRequests.TryDequeue(out def);

                if (def.Victim != null)
                {
                    def.Victim.DoDamage(def.Damage, def.DamageType, def.Sync, def.Hit, def.AttackerId);

                    if (def.ImpulseEntity?.Physics != null && def.ImpulseForce != null && def.ImpulsePosition != null)
                    {
                        def.ImpulseEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, def.ImpulseForce, def.ImpulsePosition, null);
                    }

                }
            }
        }

        public override void Draw()
        {
            foreach (Projectile p in ActiveProjectiles)
            {
                if (!p.HasExpired)
                {
                    p.Draw();
                }
            }
        }
    }
}

