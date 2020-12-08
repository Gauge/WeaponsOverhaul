using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using SENetworkAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
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
	public class WeaponControlLayer : MyGameLogicComponent
	{
		public static bool HijackComplete = false;
		public WeaponBase Weapon = new WeaponBase();
		public bool Blacklisted = false;
		private bool waitframe = true;

		/// <summary>
		/// This fires before the init function so i am using it instead of init
		/// </summary>
		public override void OnAddedToContainer()
		{
			if (!Weapon.Initialized)
			{
				Weapon.Init(this);
				Settings.OnSettingsUpdated += SystemRestart;
			}

			if (Entity.InScene)
			{
				OnAddedToScene();
			}
		}

		public override void OnAddedToScene()
		{
			base.OnAddedToScene();
		}

		public override void Init(MyObjectBuilder_EntityBase objectBuilder)
		{
			Blacklisted = IsThisBlockBlacklisted(Entity);

			if (Blacklisted)
			{
				MarkForClose();
				return;
			}

			Weapon.Start();

			if (Settings.Initialized)
				SystemRestart();

			NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME; 
		}

		public override void UpdateOnceBeforeFrame()
		{
			if (waitframe)
			{
				waitframe = false;
				NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
				return;
			}

			HijackSystem();
		}

		public override void UpdateBeforeSimulation()
		{
			Weapon.Update();
		}

		public override void UpdateAfterSimulation() 
		{
			Weapon.Animate();
		}

		public virtual void SystemRestart()
		{
			Weapon.SystemRestart();
		}

		public override void Close()
		{
			Settings.OnSettingsUpdated -= SystemRestart;
			Weapon.Close();
			base.Close();
		}

		public static bool IsThisBlockBlacklisted(IMyEntity Entity)
		{
			return Settings.Static.Blacklist.Contains($"{((Entity as IMyCubeBlock).SlimBlock.BlockDefinition as MyWeaponBlockDefinition).WeaponDefinitionId.SubtypeId.String}");
		}

		public static void HijackSystem()
		{
			if (HijackComplete)
				return;

			Action<MyEntity> old = MyEntitiesInterface.RegisterUpdate;
			MyEntitiesInterface.RegisterUpdate = (e) =>
			{
				if (e.GameLogic.GetAs<WeaponControlLayer>() != null && e.NeedsUpdate != MyEntityUpdateEnum.NONE)
				{
					e.NeedsUpdate = MyEntityUpdateEnum.NONE;
				}

				old?.Invoke(e);
			};

			OverrideDefaultControls<IMySmallGatlingGun>();
			OverrideDefaultControls<IMyLargeTurretBase>();

			HijackComplete = true;
		}

		private static void OverrideDefaultControls<T>()
		{
			Action<IMyTerminalBlock> oldAction;
			Action<IMyTerminalBlock, StringBuilder> oldWriter;
			Func<IMyTerminalBlock, bool> oldGetter;
			Action<IMyTerminalBlock, bool> oldSetter;

			List<IMyTerminalAction> actions = new List<IMyTerminalAction>();
			MyAPIGateway.TerminalControls.GetActions<T>(out actions);
			foreach (IMyTerminalAction a in actions)
			{
				if (a.Id == "Shoot")
				{
					oldAction = a.Action;
					oldWriter = a.Writer;
					a.Action = (block) => {
						try
						{
							WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
							if (logic != null)
							{
								WeaponBase wb = logic.Weapon;
								if (wb == null)
									return;

								wb.State.Value ^= WeaponState.TerminalShoot;
							}
							else
							{
								oldAction?.Invoke(block);
							}
						}
						catch (Exception e)
						{
							Tools.Warning($"Failed the shoot on/off action\n {e}");
						}
					};

					a.Writer = (block, text) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							WeaponsFiringWriter(logic.Weapon, text);
						}
						else
						{
							oldWriter?.Invoke(block, text);
						}
					};
				}
				else if (a.Id == "ShootOnce")
				{
					oldAction = a.Action;
					a.Action = (block) => {
						try
						{
							WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
							if (logic != null)
							{
								WeaponBase wb = logic.Weapon;

								if (wb == null)
									return;

								wb.State.Value |= WeaponState.TerminalShootOnce;
							}
							else
							{
								oldAction?.Invoke(block);
							}
						}
						catch (Exception e)
						{
							Tools.Warning($"Failed the shoot once action\n {e}");
						}
					};
				}
				if (a.Id == "Shoot_On")
				{
					oldAction = a.Action;
					oldWriter = a.Writer;
					a.Action = (block) => {
						try
						{
							WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
							if (logic != null)
							{
								WeaponBase wb = logic.Weapon;
								if (wb == null)
									return;

								wb.State.Value |= WeaponState.TerminalShoot;
							}
							else
							{
								oldAction?.Invoke(block);
							}
						}
						catch (Exception e)
						{
							Tools.Warning($"Failed the shoot on action\n {e}");
						}
					};

					a.Writer = (block, text) => {

						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							WeaponsFiringWriter(logic.Weapon, text);
						}
						else
						{
							oldWriter?.Invoke(block, text);
						}
					};

				}
				else if (a.Id == "Shoot_Off")
				{
					oldAction = a.Action;
					oldWriter = a.Writer;
					a.Action = (block) => {
						try
						{
							WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
							if (logic != null)
							{
								WeaponBase wb = logic.Weapon as WeaponBase;

								if (wb == null)
									return;

								wb.State.Value &= ~WeaponState.TerminalShoot;
							}
							else
							{
								oldAction?.Invoke(block);
							}
						}
						catch (Exception e)
						{
							Tools.Warning($"Failed the shoot off action\n {e}");
						}
					};

					a.Writer = (block, text) => {

						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							WeaponsFiringWriter(logic.Weapon, text);
						}
						else
						{
							oldWriter?.Invoke(block, text);
						}
					};
				}
			}

			List<IMyTerminalControl> controls = new List<IMyTerminalControl>();
			MyAPIGateway.TerminalControls.GetControls<T>(out controls);
			foreach (IMyTerminalControl c in controls)
			{
				Tools.Debug($"{c.Id}");
				if (c.Id == "Shoot")
				{
					IMyTerminalControlOnOffSwitch onoff = c as IMyTerminalControlOnOffSwitch;
					oldGetter = onoff.Getter;
					oldSetter = onoff.Setter;

					onoff.Setter = (block, value) => {
						try
						{
							WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
							if (logic != null)
							{
								WeaponBase wb = logic.Weapon as WeaponBase;
								if (wb == null)
									return;

								wb.State.Value ^= WeaponState.TerminalShoot;
							}
							else
							{
								oldSetter?.Invoke(block, value);
							}
						}
						catch (Exception e)
						{
							Tools.Warning($"Failed to toggle Shoot On/Off terminal control\n {e.ToString()}");
						}
					};

					onoff.Getter = (block) => {
						try
						{
							WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
							if (logic != null)
							{
								return (logic.Weapon.State.Value & WeaponState.TerminalShoot) == WeaponState.TerminalShoot;
							}
							else
							{
								return oldGetter.Invoke(block);
							}
						}
						catch (Exception e)
						{
							Tools.Warning($"Failed to get the Shoot On/Off terminal control\n {e}");
							return false;
						}
					};
				}
				else if (c.Id == "ShootOnce")
				{
					IMyTerminalControlButton button = c as IMyTerminalControlButton;
					oldAction = button.Action;
					button.Action = (block) => {
						try
						{
							WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
							if (logic != null)
							{
								WeaponBase wb = logic.Weapon;
								if (wb == null)
									return;

								wb.State.Value |= WeaponState.TerminalShootOnce;
							}
							else
							{
								oldAction?.Invoke(block);
							}
						}
						catch (Exception e)
						{
							Tools.Warning($"Failed the shoot once action\n {e}");
						}
					};
				}
			}
		}

		private static void WeaponsFiringWriter(WeaponBase wb, StringBuilder str)
		{
			if (wb != null && (wb.State.Value & WeaponState.TerminalShoot) == WeaponState.TerminalShoot)
			{
				str.Append("On");
			}
			else
			{
				str.Append("Off");
			}
		}
	}
}
