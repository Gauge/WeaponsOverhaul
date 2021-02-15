using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
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
		public override void OnAddedToContainer()
		{
			Weapon = new TurretBase();
			base.OnAddedToContainer();
		}
	}

	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallGatlingGun), false)]
	public class FixedGun : WeaponControlLayer
	{
		public override void OnAddedToContainer()
		{
			Weapon = new WeaponBase();
			base.OnAddedToContainer();
		}
	}

	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_InteriorTurret), false)]
	public class InteriorTurret : WeaponControlLayer
	{
		public override void OnAddedToContainer()
		{
			Weapon = new TurretBase();
			base.OnAddedToContainer();
		}
	}

	/// <summary>
	/// Binds keens interface to the custom weapon types
	/// </summary>
	public class WeaponControlLayer : MyGameLogicComponent
	{
		public static bool HijackUpdates = false;
		public static bool HijackSmallGatlingGun = false;
		public static bool HijackLargeTurretBase = false;

		public WeaponBase Weapon = null;
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

			IMyCubeBlock block = Entity as IMyCubeBlock;
			if (block.CubeGrid.Physics == null || !block.CubeGrid.Physics.Enabled)
				return;
			NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
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

		public void HijackSystem()
		{
			if (!HijackUpdates)
			{
				Tools.Debug($"Hijacking event system");
				Action<MyEntity> old = MyEntitiesInterface.RegisterUpdate;
				MyEntitiesInterface.RegisterUpdate = (e) => {
					if (e.GameLogic.GetAs<WeaponControlLayer>() != null && e.NeedsUpdate != MyEntityUpdateEnum.NONE)
					{
						if ((e.NeedsUpdate & MyEntityUpdateEnum.BEFORE_NEXT_FRAME) != 0)
						{
							e.NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
						}
						else
						{
							e.NeedsUpdate = MyEntityUpdateEnum.NONE;
						}
					}

					old?.Invoke(e);
				};

				HijackUpdates = true;
			}


			if (Entity is IMySmallGatlingGun && !HijackSmallGatlingGun)
			{
				Tools.Debug($"Hijacking fixed guns");
				OverrideDefaultControls<IMySmallGatlingGun>();
				HijackSmallGatlingGun = true;
			}

			if (Entity is IMyLargeTurretBase && !HijackLargeTurretBase)
			{
				Tools.Debug($"Hijacking turrets");
				OverrideDefaultControls<IMyLargeTurretBase>();
				HijackLargeTurretBase = true;
			}
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
				Tools.Debug($"{a.Id}");
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

				else if (a.Id == "Control")
				{
					//oldAction = a.Action;
					//oldWriter = a.Writer;

					//a.Action = (block) => {
					//	try
					//	{
					//		WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
					//		if (logic != null)
					//		{
					//			TurretBase tb = logic.Weapon as TurretBase;

					//			if (tb == null)
					//				return;

					//			tb.TakeControl();
					//		}
					//		else
					//		{
					//			oldAction?.Invoke(block);
					//		}
					//	}
					//	catch (Exception e)
					//	{
					//		Tools.Warning($"Failed to take control of turret\n {e}");
					//	}
					//};
				}

				else if (a.Id == "IncreaseRange")
				{

				}
				else if (a.Id == "DecreaseRange")
				{

				}
				else if (a.Id == "EnableIdleMovement")
				{

				}
				else if (a.Id == "EnableIdleMovement_On")
				{

				}
				else if (a.Id == "EnableIdleMovement_Off")
				{

				}

				else if (a.Id == "TargetMeteors")
				{
					oldAction = a.Action;
					oldWriter = a.Writer;

					a.Action = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetMeteors = !tb.TargetMeteors;
						}
						else
						{
							oldAction?.Invoke(block);
						}
					};

					a.Writer = (block, text) => {

						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							text.Append((tb.TargetMeteors) ? "On" : "Off");
						}
						else
						{
							oldWriter?.Invoke(block, text);
						}
					};

				}
				else if (a.Id == "TargetMeteors_On")
				{
					oldAction = a.Action;
					oldWriter = a.Writer;

					a.Action = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetMeteors = true;
						}
						else
						{
							oldAction?.Invoke(block);
						}
					};

					a.Writer = (block, text) => {

						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							text.Append((tb.TargetMeteors) ? "On" : "Off");
						}
						else
						{
							oldWriter?.Invoke(block, text);
						}
					};
				}
				else if (a.Id == "TargetMeteors_Off")
				{
					oldAction = a.Action;
					oldWriter = a.Writer;

					a.Action = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetMeteors = false;
						}
						else
						{
							oldAction?.Invoke(block);
						}
					};

					a.Writer = (block, text) => {

						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							text.Append((tb.TargetMeteors) ? "On" : "Off");
						}
						else
						{
							oldWriter?.Invoke(block, text);
						}
					};
				}

				else if (a.Id == "TargetMissiles")
				{
					oldAction = a.Action;
					oldWriter = a.Writer;

					a.Action = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetMissiles = !tb.TargetMissiles;
						}
						else
						{
							oldAction?.Invoke(block);
						}
					};

					a.Writer = (block, text) => {

						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							text.Append((tb.TargetMissiles) ? "On" : "Off");
						}
						else
						{
							oldWriter?.Invoke(block, text);
						}
					};
				}
				else if (a.Id == "TargetMissiles_On")
				{
					oldAction = a.Action;
					oldWriter = a.Writer;

					a.Action = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetMissiles = true;
						}
						else
						{
							oldAction?.Invoke(block);
						}
					};

					a.Writer = (block, text) => {

						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							text.Append((tb.TargetMissiles) ? "On" : "Off");
						}
						else
						{
							oldWriter?.Invoke(block, text);
						}
					};
				}
				else if (a.Id == "TargetMissiles_Off")
				{
					oldAction = a.Action;
					oldWriter = a.Writer;

					a.Action = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetMissiles = false;
						}
						else
						{
							oldAction?.Invoke(block);
						}
					};

					a.Writer = (block, text) => {

						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							text.Append((tb.TargetMissiles) ? "On" : "Off");
						}
						else
						{
							oldWriter?.Invoke(block, text);
						}
					};
				}

				else if (a.Id == "TargetSmallShips")
				{
					oldAction = a.Action;
					oldWriter = a.Writer;

					a.Action = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetSmallShips = !tb.TargetSmallShips;
						}
						else
						{
							oldAction?.Invoke(block);
						}
					};

					a.Writer = (block, text) => {

						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							text.Append((tb.TargetSmallShips) ? "On" : "Off");
						}
						else
						{
							oldWriter?.Invoke(block, text);
						}
					};
				}
				else if (a.Id == "TargetSmallShips_On")
				{
					oldAction = a.Action;
					oldWriter = a.Writer;

					a.Action = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetSmallShips = true;
						}
						else
						{
							oldAction?.Invoke(block);
						}
					};

					a.Writer = (block, text) => {

						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							text.Append((tb.TargetSmallShips) ? "On" : "Off");
						}
						else
						{
							oldWriter?.Invoke(block, text);
						}
					};
				}
				else if (a.Id == "TargetSmallShips_Off")
				{
					oldAction = a.Action;
					oldWriter = a.Writer;

					a.Action = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetSmallShips = false;
						}
						else
						{
							oldAction?.Invoke(block);
						}
					};

					a.Writer = (block, text) => {

						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							text.Append((tb.TargetSmallShips) ? "On" : "Off");
						}
						else
						{
							oldWriter?.Invoke(block, text);
						}
					};
				}

				else if (a.Id == "TargetLargeShips")
				{
					oldAction = a.Action;
					oldWriter = a.Writer;

					a.Action = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetLargeShips = !tb.TargetLargeShips;
						}
						else
						{
							oldAction?.Invoke(block);
						}
					};

					a.Writer = (block, text) => {

						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							text.Append((tb.TargetLargeShips) ? "On" : "Off");
						}
						else
						{
							oldWriter?.Invoke(block, text);
						}
					};
				}
				else if (a.Id == "TargetLargeShips_On")
				{
					oldAction = a.Action;
					oldWriter = a.Writer;

					a.Action = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetLargeShips = true;
						}
						else
						{
							oldAction?.Invoke(block);
						}
					};

					a.Writer = (block, text) => {

						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							text.Append((tb.TargetLargeShips) ? "On" : "Off");
						}
						else
						{
							oldWriter?.Invoke(block, text);
						}
					};
				}
				else if (a.Id == "TargetLargeShips_Off")
				{
					oldAction = a.Action;
					oldWriter = a.Writer;

					a.Action = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetLargeShips = false;
						}
						else
						{
							oldAction?.Invoke(block);
						}
					};

					a.Writer = (block, text) => {

						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							text.Append((tb.TargetLargeShips) ? "On" : "Off");
						}
						else
						{
							oldWriter?.Invoke(block, text);
						}
					};
				}

				else if (a.Id == "TargetCharacters")
				{
					oldAction = a.Action;
					oldWriter = a.Writer;

					a.Action = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetCharacters = !tb.TargetCharacters;
						}
						else
						{
							oldAction?.Invoke(block);
						}
					};

					a.Writer = (block, text) => {

						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							text.Append((tb.TargetCharacters) ? "On" : "Off");
						}
						else
						{
							oldWriter?.Invoke(block, text);
						}
					};
				}
				else if (a.Id == "TargetCharacters_On")
				{
					oldAction = a.Action;
					oldWriter = a.Writer;

					a.Action = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetCharacters = true;
						}
						else
						{
							oldAction?.Invoke(block);
						}
					};

					a.Writer = (block, text) => {

						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							text.Append((tb.TargetCharacters) ? "On" : "Off");
						}
						else
						{
							oldWriter?.Invoke(block, text);
						}
					};
				}
				else if (a.Id == "TargetCharacters_Off")
				{
					oldAction = a.Action;
					oldWriter = a.Writer;

					a.Action = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetCharacters = false;
						}
						else
						{
							oldAction?.Invoke(block);
						}
					};

					a.Writer = (block, text) => {

						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							text.Append((tb.TargetCharacters) ? "On" : "Off");
						}
						else
						{
							oldWriter?.Invoke(block, text);
						}
					};
				}

				else if (a.Id == "TargetStations")
				{
					oldAction = a.Action;
					oldWriter = a.Writer;

					a.Action = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetStations = !tb.TargetStations;
						}
						else
						{
							oldAction?.Invoke(block);
						}
					};

					a.Writer = (block, text) => {

						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							text.Append((tb.TargetStations) ? "On" : "Off");
						}
						else
						{
							oldWriter?.Invoke(block, text);
						}
					};
				}
				else if (a.Id == "TargetStations_On")
				{
					oldAction = a.Action;
					oldWriter = a.Writer;

					a.Action = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetStations = true;
						}
						else
						{
							oldAction?.Invoke(block);
						}
					};

					a.Writer = (block, text) => {

						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							text.Append((tb.TargetStations) ? "On" : "Off");
						}
						else
						{
							oldWriter?.Invoke(block, text);
						}
					};
				}
				else if (a.Id == "TargetStations_Off")
				{
					oldAction = a.Action;
					oldWriter = a.Writer;

					a.Action = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetStations = false;
						}
						else
						{
							oldAction?.Invoke(block);
						}
					};

					a.Writer = (block, text) => {

						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							text.Append((tb.TargetStations) ? "On" : "Off");
						}
						else
						{
							oldWriter?.Invoke(block, text);
						}
					};
				}

				else if (a.Id == "TargetNeutrals")
				{
					oldAction = a.Action;
					oldWriter = a.Writer;

					a.Action = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetNeutrals = !tb.TargetNeutrals;
						}
						else
						{
							oldAction?.Invoke(block);
						}
					};

					a.Writer = (block, text) => {

						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							text.Append((tb.TargetNeutrals) ? "On" : "Off");
						}
						else
						{
							oldWriter?.Invoke(block, text);
						}
					};
				}
				else if (a.Id == "TargetNeutrals_On")
				{
					oldAction = a.Action;
					oldWriter = a.Writer;

					a.Action = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetNeutrals = true;
						}
						else
						{
							oldAction?.Invoke(block);
						}
					};

					a.Writer = (block, text) => {

						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							text.Append((tb.TargetNeutrals) ? "On" : "Off");
						}
						else
						{
							oldWriter?.Invoke(block, text);
						}
					};
				}
				else if (a.Id == "TargetNeutrals_Off")
				{
					oldAction = a.Action;
					oldWriter = a.Writer;

					a.Action = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetNeutrals = false;
						}
						else
						{
							oldAction?.Invoke(block);
						}
					};

					a.Writer = (block, text) => {

						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							text.Append((tb.TargetNeutrals) ? "On" : "Off");
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
					};

					onoff.Getter = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							return (logic.Weapon.State.Value & WeaponState.TerminalShoot) == WeaponState.TerminalShoot;
						}
						else
						{
							return oldGetter.Invoke(block);
						}
					};
				}
				else if (c.Id == "ShootOnce")
				{
					IMyTerminalControlButton button = c as IMyTerminalControlButton;
					oldAction = button.Action;
					button.Action = (block) => {
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
					};
				}

				else if (c.Id == "Control")
				{ }
				else if (c.Id == "Range")
				{ }
				else if (c.Id == "EnableIdleMovement")
				{ }
				else if (c.Id == "TargetMeteors")
				{
					IMyTerminalControlOnOffSwitch onoff = c as IMyTerminalControlOnOffSwitch;
					oldGetter = onoff.Getter;
					oldSetter = onoff.Setter;

					onoff.Setter = (block, value) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetMeteors = !tb.TargetMeteors;
						}
						else
						{
							oldSetter?.Invoke(block, value);
						}
					};

					onoff.Getter = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return false;

							return tb.TargetMeteors;
						}
						else
						{
							return oldGetter.Invoke(block);
						}
					};
				}
				else if (c.Id == "TargetMissiles")
				{
					IMyTerminalControlOnOffSwitch onoff = c as IMyTerminalControlOnOffSwitch;
					oldGetter = onoff.Getter;
					oldSetter = onoff.Setter;

					onoff.Setter = (block, value) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetMissiles = !tb.TargetMissiles;
						}
						else
						{
							oldSetter?.Invoke(block, value);
						}
					};

					onoff.Getter = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return false;

							return tb.TargetMissiles;
						}
						else
						{
							return oldGetter.Invoke(block);
						}
					};
				}
				else if (c.Id == "TargetSmallShips")
				{
					IMyTerminalControlOnOffSwitch onoff = c as IMyTerminalControlOnOffSwitch;
					oldGetter = onoff.Getter;
					oldSetter = onoff.Setter;

					onoff.Setter = (block, value) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetSmallShips = !tb.TargetSmallShips;
						}
						else
						{
							oldSetter?.Invoke(block, value);
						}
					};

					onoff.Getter = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return false;

							return tb.TargetSmallShips;
						}
						else
						{
							return oldGetter.Invoke(block);
						}
					};
				}
				else if (c.Id == "TargetLargeShips")
				{
					IMyTerminalControlOnOffSwitch onoff = c as IMyTerminalControlOnOffSwitch;
					oldGetter = onoff.Getter;
					oldSetter = onoff.Setter;

					onoff.Setter = (block, value) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetLargeShips = !tb.TargetLargeShips;
						}
						else
						{
							oldSetter?.Invoke(block, value);
						}
					};

					onoff.Getter = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return false;

							return tb.TargetLargeShips;
						}
						else
						{
							return oldGetter.Invoke(block);
						}
					};
				}
				else if (c.Id == "TargetCharacters")
				{
					IMyTerminalControlOnOffSwitch onoff = c as IMyTerminalControlOnOffSwitch;
					oldGetter = onoff.Getter;
					oldSetter = onoff.Setter;

					onoff.Setter = (block, value) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetCharacters = !tb.TargetCharacters;
						}
						else
						{
							oldSetter?.Invoke(block, value);
						}
					};

					onoff.Getter = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return false;

							return tb.TargetCharacters;
						}
						else
						{
							return oldGetter.Invoke(block);
						}
					};
				}
				else if (c.Id == "TargetStations")
				{
					IMyTerminalControlOnOffSwitch onoff = c as IMyTerminalControlOnOffSwitch;
					oldGetter = onoff.Getter;
					oldSetter = onoff.Setter;

					onoff.Setter = (block, value) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetStations = !tb.TargetStations;
						}
						else
						{
							oldSetter?.Invoke(block, value);
						}
					};

					onoff.Getter = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return false;

							return tb.TargetStations;
						}
						else
						{
							return oldGetter.Invoke(block);
						}
					};
				}
				else if (c.Id == "TargetNeutrals")
				{
					IMyTerminalControlOnOffSwitch onoff = c as IMyTerminalControlOnOffSwitch;
					oldGetter = onoff.Getter;
					oldSetter = onoff.Setter;

					onoff.Setter = (block, value) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return;

							tb.TargetNeutrals = !tb.TargetNeutrals;
						}
						else
						{
							oldSetter?.Invoke(block, value);
						}
					};

					onoff.Getter = (block) => {
						WeaponControlLayer logic = block.GameLogic.GetAs<WeaponControlLayer>();
						if (logic != null)
						{
							TurretBase tb = logic.Weapon as TurretBase;
							if (tb == null)
								return false;

							return tb.TargetNeutrals;
						}
						else
						{
							return oldGetter.Invoke(block);
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
