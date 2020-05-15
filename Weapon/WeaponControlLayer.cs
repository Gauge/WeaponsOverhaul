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
		public static bool ControlsInitialized = false;
		public WeaponBase Weapon = new WeaponBase();

		/// <summary>
		/// This fires before the init function so i am using it instead of init
		/// </summary>
		public override void OnAddedToContainer()
		{
			base.OnAddedToContainer();
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
			base.Init(objectBuilder);

			//if (IsThisBlockBlacklisted(Entity))
			//{
			//	MarkForClose();
			//	return;
			//}

			Weapon.Start();

			TerminalIntitalize();

			if (Settings.Initialized)
				SystemRestart();
		}

		public override void UpdateBeforeSimulation()
		{
			base.UpdateBeforeSimulation();

			Weapon.Update();
			Weapon.Spawn();
			
		}

		public override void UpdateAfterSimulation() 
		{
			Weapon.Animate();
		}

		public virtual void SystemRestart()
		{
			Weapon.SystemRestart();
			if (MyAPIGateway.Session.WeaponsEnabled)
				NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
		}

		public override void Close()
		{
			Settings.OnSettingsUpdated -= SystemRestart;
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
			if (ControlsInitialized)
				return;

			OverrideDefaultControls<IMySmallGatlingGun>();
			OverrideDefaultControls<IMyLargeTurretBase>();

			ControlsInitialized = true;
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

								wb.TerminalShooting.Value = !wb.TerminalShooting.Value;
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

								if (wb != null && !wb.TerminalShootOnce.Value)
								{
									wb.TerminalShootOnce.Value = true;
								}
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

								if (wb != null && !wb.TerminalShooting.Value)
								{
									wb.TerminalShooting.Value = true;
								}
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

								if (wb != null && wb.TerminalShooting.Value)
								{

									wb.TerminalShooting.Value = false;
								}
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

								if (wb != null && wb.TerminalShooting.Value != value)
								{
									wb.TerminalShooting.Value = value;
								}
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
								return logic.Weapon.TerminalShooting.Value;
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

								if (wb != null && !wb.TerminalShootOnce.Value)
								{
									wb.TerminalShootOnce.Value = true;
								}
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
			if (wb != null && wb.TerminalShooting.Value)
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
