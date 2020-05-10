using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using SENetworkAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace WeaponsOverhaul
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeGatlingTurret), false)]
    public class Turret : WeaponControlLayer
    {
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallGatlingGun), false)]
    public class FixedGun : WeaponControlLayer
    {
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_InteriorTurret), false)]
    public class InteriorTurret : WeaponControlLayer
    {
    }

    /// <summary>
    /// Binds keens interface to the custom weapon types
    /// </summary>
    public class WeaponControlLayer : MyNetworkGameLogicComponent
    {
        public static bool DefaultTerminalControlsInitialized = false;
        public static Action<IMyTerminalBlock> TerminalShootActionTurretBase;
        public static Action<IMyTerminalBlock, StringBuilder> TerminalShootWriterTurretBase;
        public static Action<IMyTerminalBlock> TerminalShootOnceActionTurretBase;
        public static Action<IMyTerminalBlock, StringBuilder> TerminalShootOnceWriterTurretBase;
        public static Action<IMyTerminalBlock> TerminalShootOnActionTurretBase;
        public static Action<IMyTerminalBlock, StringBuilder> TerminalShootOnWriterTurretBase;
        public static Action<IMyTerminalBlock> TerminalShootOffActionTurretBase;
        public static Action<IMyTerminalBlock, StringBuilder> TerminalShootOffWriterTurretBase;
        public static Action<IMyTerminalBlock, bool> TerminalShootSetterTurretBase;
        //public static Action<IMyTerminalBlock, StringBuilder> TerminalShootSetterWriterTurretBase;
        public static Func<IMyTerminalBlock, bool> TerminalShootGetterTurretBase;
        //public static Action<IMyTerminalBlock, StringBuilder> TerminalShootGetterWriterTurretBase;

        public static Action<IMyTerminalBlock> TerminalShootActionGatlingGun;
        public static Action<IMyTerminalBlock, StringBuilder> TerminalShootWriterGatlingGun;
        public static Action<IMyTerminalBlock> TerminalShootOnceActionGatlingGun;
        public static Action<IMyTerminalBlock, StringBuilder> TerminalShootOnceWriterGatlingGun;
        public static Action<IMyTerminalBlock> TerminalShootOnActionGatlingGun;
        public static Action<IMyTerminalBlock, StringBuilder> TerminalShootOnWriterGatlingGun;
        public static Action<IMyTerminalBlock> TerminalShootOffActionGatlingGun;
        public static Action<IMyTerminalBlock, StringBuilder> TerminalShootOffWriterGatlingGun;
        public static Action<IMyTerminalBlock, bool> TerminalShootSetterGatlingGun;
        //public static Action<IMyTerminalBlock, StringBuilder> TerminalShootSetterWriterGatlingGun;
        public static Func<IMyTerminalBlock, bool> TerminalShootGetterGatlingGun;
        //public static Action<IMyTerminalBlock, StringBuilder> TerminalShootGetterWriterGatlingGun;

        public WeaponBase Weapon = new WeaponBase();

        /// <summary>
        /// This fires before the init function so i am using it instead of init
        /// </summary>
        public override void OnAddedToContainer()
        {
            if (!Weapon.Initialized)
            {
                Weapon.Init(this);
                Core.OnSettingsUpdated -= SystemRestart;
                Core.OnSettingsUpdated += SystemRestart;
            }

            Weapon.OnAddedToContainer();

            if (Entity.InScene)
            {
                OnAddedToScene();
            }
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene(); // the NetworkAPI requires this.
            if (IsThisBlockBlacklisted(Entity))
            {
                MarkForClose();
                return;
            }

            Weapon.OnAddedToScene();
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (Settings.Initialized)
                SystemRestart();
        }

        public override void OnRemovedFromScene()
        {
            Weapon.OnRemovedFromScene();
        }

        public override void OnBeforeRemovedFromContainer()
        {
            Weapon.OnBeforeRemovedFromContainer();
        }

        public override void MarkForClose()
        {
            Weapon.MarkForClose();
        }

        public override void UpdateAfterSimulation()
        {
            Weapon.UpdateAfterSimulation();
        }

        public override void UpdateAfterSimulation10()
        {
            Weapon.UpdateAfterSimulation10();
        }

        public override void UpdateAfterSimulation100()
        {
            Weapon.UpdateAfterSimulation100();
        }

        public override void UpdateBeforeSimulation()
        {
            Weapon.UpdateBeforeSimulation();
        }

        public override void UpdateBeforeSimulation10()
        {
            Weapon.UpdateBeforeSimulation10();
        }

        public override void UpdateBeforeSimulation100()
        {
            Weapon.UpdateBeforeSimulation100();
        }

        public override void UpdateOnceBeforeFrame()
        {
            Weapon.UpdateOnceBeforeFrame();
        }

        public override void UpdatingStopped()
        {
            Weapon.UpdatingStopped();
        }

        //public void OnSettingsUpdated()
        //{
        //    if (SettingsJustUpdated)
        //    {
        //        SettingsJustUpdated = false;
        //        return;
        //    }

        //    SettingsJustUpdated = true;

        //    MyWeaponDefinition w = MyDefinitionManager.Static.GetWeaponDefinition(((Entity as IMyFunctionalBlock).SlimBlock.BlockDefinition as MyWeaponBlockDefinition).WeaponDefinitionId);
        //    WeaponDefinition definition = Settings.GetWeaponDefinition(w.Id.SubtypeId.String);

        //    WeaponBasic basic = new WeaponBasic();
        //    definition.Clone(basic);

        //    switch (definition.Type())
        //    {
        //        case WeaponType.Ramping:
        //            Weapon = new WeaponRamping();
        //            (Weapon as WeaponRamping).Set(definition);
        //            break;
        //        case WeaponType.Basic:
        //            Weapon = new WeaponBasic();
        //            (Weapon as WeaponBasic).Set(definition);
        //            break;
        //    }

        //    Weapon.Init((MyEntity)Entity);
        //    OnAddedToContainer();

        //    NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
        //}

        public virtual void SystemRestart() 
        {
            if (IsThisBlockBlacklisted(Entity)) return;

            Weapon.SystemRestart();
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void Close()
        {
            Core.OnSettingsUpdated -= SystemRestart;
            Weapon.Close();
            base.Close();
        }

        public static bool IsThisBlockBlacklisted(IMyEntity Entity)
        {
            //MyDefinitionBase def = (Entity as IMyTerminalBlock).SlimBlock.BlockDefinition;
            //return Settings.BlackList.Contains(def.Id.SubtypeName);
            return false;
        }

        public static void TerminalIntitalize()
        {
            List<IMyTerminalAction> actions = new List<IMyTerminalAction>();
            MyAPIGateway.TerminalControls.GetActions<IMyLargeTurretBase>(out actions);

            foreach (IMyTerminalAction a in actions)
            {
                if (a.Id == "Shoot")
                {
                    TerminalShootActionTurretBase = a.Action;
                    TerminalShootWriterTurretBase = a.Writer;
                }
                else if (a.Id == "ShootOnce")
                {
                    TerminalShootOnceActionTurretBase = a.Action;
                    TerminalShootOnceWriterTurretBase = a.Writer;
                }
                if (a.Id == "Shoot_On")
                {
                    TerminalShootOnActionTurretBase = a.Action;
                    TerminalShootOnWriterTurretBase = a.Writer;
                }
                else if (a.Id == "Shoot_Off")
                {
                    TerminalShootOffActionTurretBase = a.Action;
                    TerminalShootOffWriterTurretBase = a.Writer;
                }
            }

            actions.Clear();
            MyAPIGateway.TerminalControls.GetActions<IMySmallGatlingGun>(out actions);
            foreach (IMyTerminalAction a in actions)
            {
                if (a.Id == "Shoot")
                {
                    TerminalShootActionGatlingGun = a.Action;
                    TerminalShootWriterGatlingGun = a.Writer;
                }
                else if (a.Id == "ShootOnce")
                {
                    TerminalShootOnceActionGatlingGun = a.Action;
                    TerminalShootOnceWriterGatlingGun = a.Writer;
                }
                if (a.Id == "Shoot_On")
                {
                    TerminalShootOnActionGatlingGun = a.Action;
                    TerminalShootOnWriterGatlingGun = a.Writer;
                }
                else if (a.Id == "Shoot_Off")
                {
                    TerminalShootOffActionGatlingGun = a.Action;
                    TerminalShootOffWriterGatlingGun = a.Writer;
                }
            }

            List<IMyTerminalControl> controls = new List<IMyTerminalControl>();
            MyAPIGateway.TerminalControls.GetControls<IMyLargeTurretBase>(out controls);
            foreach (IMyTerminalControl c in controls)
            {
                if (c.Id == "Shoot")
                {
                    IMyTerminalControlOnOffSwitch onoff = c as IMyTerminalControlOnOffSwitch;
                    TerminalShootGetterTurretBase = onoff.Getter;
                    TerminalShootSetterTurretBase = onoff.Setter;
                }
            }


            controls.Clear();
            MyAPIGateway.TerminalControls.GetControls<IMySmallGatlingGun>(out controls);
            foreach (IMyTerminalControl c in controls)
            {
                if (c.Id == "Shoot")
                {
                    IMyTerminalControlOnOffSwitch onoff = c as IMyTerminalControlOnOffSwitch;
                    TerminalShootGetterGatlingGun = onoff.Getter;
                    TerminalShootSetterGatlingGun = onoff.Setter;
                }
            }

            DefaultTerminalControlsInitialized = true;
        }
    }
}
