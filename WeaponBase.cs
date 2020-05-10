using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
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
using VRageMath;

namespace WeaponsOverhaul
{
	public class WeaponBase : WeaponDefinition
	{
		public bool Initialized { get; private set; } = false;
		public bool IsFixedGun { get; private set; }
		public bool IsShooting => gun.IsShooting || TerminalShootOnce.Value || TerminalShooting.Value || (IsFixedGun && (Entity.NeedsUpdate & MyEntityUpdateEnum.EACH_FRAME) == MyEntityUpdateEnum.EACH_FRAME);

		protected static bool ControlsUpdated;

		protected NetSync<bool> TerminalShootOnce;
		protected NetSync<bool> TerminalShooting;
		


		protected bool WillFireThisFrame;

		protected int CurrentShotInBurst = 0;
		protected float CurrentReloadTime = 0;
		protected float CurrentReleaseTime = 0;
		protected double TimeTillNextShot = 1d;
		protected float CurrentIdleReloadTime = 0;


		protected WeaponControlLayer ControlLayer;
		protected MyEntity Entity;
		protected IMyFunctionalBlock Block;
		protected IMyCubeBlock Cube;
		protected IMyGunObject<MyGunBase> gun;

		public void Init(WeaponControlLayer layer)
		{
			ControlLayer = layer;
			Entity = (MyEntity)layer.Entity;
			Block = Entity as IMyFunctionalBlock;
			Cube = Entity as IMyCubeBlock;
			gun = Entity as IMyGunObject<MyGunBase>;
			IsFixedGun = Entity is IMySmallGatlingGun;

			TerminalShootOnce = new NetSync<bool>(ControlLayer, TransferType.Both, false);
			TerminalShooting = new NetSync<bool>(ControlLayer, TransferType.Both, false);

			Initialized = true;
		}

		/// <summary>
		/// Used to update the definition when syncing to the server
		/// </summary>
		public virtual void SystemRestart()
		{
			WeaponDefinition d = Settings.GetWeaponDefinition(((Entity as IMyFunctionalBlock).SlimBlock.BlockDefinition as MyWeaponBlockDefinition).WeaponDefinitionId.SubtypeId.String);
			Copy(d);

			WillFireThisFrame = false;

			CurrentShotInBurst = 0;
			CurrentReloadTime = 0;
			CurrentReleaseTime = 0;
			TimeTillNextShot = 1d;
			CurrentIdleReloadTime = 0;
		}

		public virtual void OnAddedToContainer()
		{

		}

		public virtual void OnAddedToScene()
		{
			OverrideDefaultControls();
		}

		public virtual void OnRemovedFromScene() { }

		public virtual void OnBeforeRemovedFromContainer() { }

		public virtual void MarkForClose() { }

		public virtual void UpdateAfterSimulation() { }

		public virtual void UpdateAfterSimulation10() { }

		public virtual void UpdateAfterSimulation100() { }

		public virtual void UpdateBeforeSimulation()
		{
			//if (!MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session != null)
			//{
			//	MyAPIGateway.Utilities.ShowNotification($"{(IsShooting ? "Shooting" : "Idle")}, RoF: {AmmoData.RateOfFire}, Shots: {CurrentShotInBurst}/{AmmoData.ShotsInBurst}, {(CurrentReloadTime > 0 ? $"Cooldown {(ReloadTime - CurrentReloadTime).ToString("n0")}/{ReloadTime}, " : "")}release: {CurrentReleaseTime.ToString("n0")}/{ReleaseTimeAfterFire}, Time: {TimeTillNextShot.ToString("n2")}", 1);
			//}

			// true until proven false
			WillFireThisFrame = true;

			// If cooldown is greater than 0 the gun is on cooldown and should not fire
			// reduce cooldown and dont fire projectiles
			if (CurrentReloadTime > 0)
			{
				CurrentReloadTime -= Tools.MillisecondPerFrame;
				WillFireThisFrame = false;
			}

			// if the block is not functional toggle shooting to off
			// this is not venilla and may get changed
			if (!Cube.IsWorking)
			{
				TerminalShooting.SetValue(false, SyncType.None);
				WillFireThisFrame = false;
				//StopShootingSound();
				return;
			}

			// if a user is manually shooting toggle terminal shoot off
			if (gun.IsShooting)
			{
				TerminalShooting.SetValue(false, SyncType.None);
			}

			if (!IsShooting ||
				Cube?.CubeGrid?.Physics == null ||
				!gun.GunBase.HasEnoughAmmunition() ||
				!WillFireThisFrame)
			{
				// this makes sure the gun will fire instantly when fire condisions are met
				if (TimeTillNextShot < 1)
				{
					TimeTillNextShot += AmmoData.RateOfFire * Tools.FireRateMultiplayer;
				}

				if (TimeTillNextShot > 1)
				{
					TimeTillNextShot = 1;
				}

				//MyAPIGateway.Utilities.ShowNotification($"{!IsShooting}, {Cube?.CubeGrid?.Physics == null}, {gun.GunBase.HasEnoughAmmunition()} willFire: {WillFireThisFrame}, hasEnough:  Physical: ", 1);

				WillFireThisFrame = false;
			}

			if (WillFireThisFrame)
			{
				TimeTillNextShot += AmmoData.RateOfFire * Tools.FireRateMultiplayer;
			}
			else
			{
				//StopShootingSound();
			}

			IdleReload();
			TerminalShootOnce.SetValue(false, SyncType.None);

			Spawn();
		}

		public virtual void UpdateBeforeSimulation10() { }

		public virtual void UpdateBeforeSimulation100() { }

		public virtual void UpdateOnceBeforeFrame() { }

		public virtual void UpdatingStopped() { }

		public virtual void Close() { }

		public virtual void Spawn()
		{
			if (TimeTillNextShot >= 1 && WillFireThisFrame)
			{
				MatrixD muzzleMatrix = gun.GunBase.GetMuzzleWorldMatrix();

				string ammoId = gun.GunBase.CurrentAmmoDefinition.Id.SubtypeId.String;
				AmmoDefinition ammo = Settings.GetAmmoDefinition(ammoId);

				while (TimeTillNextShot >= 1)
				{
					MatrixD positionMatrix = muzzleMatrix;
					//MatrixD positionMatrix = Matrix.CreateWorld(
					//	muzzleMatrix.Translation,
					//	Randomizer.ApplyDeviation(Entity, muzzleMatrix.Forward, DeviateShotAngle),
					//	muzzleMatrix.Up);

					Projectile bullet = new Projectile();
					bullet.ParentBlockId = Entity.EntityId;
					bullet.PartentSlim = Cube.SlimBlock;
					bullet.AmmoId = ammoId;
					bullet.InitialGridVelocity = Block.CubeGrid.Physics.LinearVelocity;
					bullet.Direction = positionMatrix.Forward;
					bullet.Velocity = Block.CubeGrid.Physics.LinearVelocity + (positionMatrix.Forward * ammo.DesiredSpeed);
					bullet.Position = positionMatrix.Translation;

					Core.SpawnProjectile(bullet);
					gun.GunBase.ConsumeAmmo();
					TimeTillNextShot--;
					//MakeShootSound();
					//MakeSecondaryShotSound();


					CurrentShotInBurst++;
					if (CurrentShotInBurst == AmmoData.ShotsInBurst)
					{
						TimeTillNextShot = 0;
						CurrentShotInBurst = 0;
						CurrentReloadTime = ReloadTime;
						break;
					}

					var forceVector = -positionMatrix.Forward * ammo.BackkickForce;
					Block.CubeGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, forceVector, Block.WorldAABB.Center, null);

					if (TerminalShootOnce.Value)
					{
						TerminalShootOnce.SetValue(false, SyncType.None);
						return;
					}

					//if (gun.GunBase.HasEnoughAmmunition())
					//{
					//	LastNoAmmoSound = 0;
					//	break;
					//}
				}
			}
		}

		public virtual void IdleReload()
		{
			if (!IsShooting && CurrentShotInBurst > 0)
			{
				if (CurrentIdleReloadTime >= ReloadTime)
				{
					CurrentShotInBurst = 0;
					CurrentIdleReloadTime = 0;
				}

				CurrentIdleReloadTime += Tools.MillisecondPerFrame;
			}
			else
			{
				CurrentIdleReloadTime = 0;
			}
		}

		protected void OverrideDefaultControls()
		{
			if (!WeaponControlLayer.DefaultTerminalControlsInitialized)
			{
				WeaponControlLayer.TerminalIntitalize();
			}

			if (WeaponControlLayer.IsThisBlockBlacklisted(Entity))
			{
				ControlLayer.MarkForClose();
				ControlsUpdated = true;
				return;
			}

			if (ControlsUpdated)
				return;

			ControlsUpdated = true;

			List<IMyTerminalAction> actions = new List<IMyTerminalAction>();

			if (Entity is IMyLargeTurretBase)
			{
				MyAPIGateway.TerminalControls.GetActions<IMyLargeTurretBase>(out actions);
			}
			else if (Entity is IMySmallGatlingGun)
			{
				MyAPIGateway.TerminalControls.GetActions<IMySmallGatlingGun>(out actions);
			}

			foreach (IMyTerminalAction a in actions)
			{
				if (a.Id == "Shoot")
				{
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
							else // below is a fallback if the block has been black listed
							{
								if (Entity is IMyLargeTurretBase)
								{
									WeaponControlLayer.TerminalShootActionTurretBase.Invoke(block);
								}
								else
								{
									WeaponControlLayer.TerminalShootActionGatlingGun.Invoke(block);
								}
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
						else // below is a fallback if the block has been black listed
						{
							if (Entity is IMyLargeTurretBase)
							{
								WeaponControlLayer.TerminalShootWriterTurretBase(block, text);
							}
							else
							{
								WeaponControlLayer.TerminalShootWriterGatlingGun(block, text);
							}
						}
					};
				}
				else if (a.Id == "ShootOnce")
				{
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
							else // below is a fallback if the block has been black listed
							{
								if (Entity is IMyLargeTurretBase)
								{
									WeaponControlLayer.TerminalShootOnceActionTurretBase.Invoke(block);
								}
								else
								{
									WeaponControlLayer.TerminalShootOnceActionGatlingGun.Invoke(block);
								}
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
							else // below is a fallback if the block has been black listed
							{
								if (Entity is IMyLargeTurretBase)
								{
									WeaponControlLayer.TerminalShootOnActionTurretBase.Invoke(block);
								}
								else
								{
									WeaponControlLayer.TerminalShootOnActionGatlingGun.Invoke(block);
								}
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
						else // below is a fallback if the block has been black listed
						{
							if (Entity is IMyLargeTurretBase)
							{
								WeaponControlLayer.TerminalShootOnWriterTurretBase(block, text);
							}
							else
							{
								WeaponControlLayer.TerminalShootOnWriterGatlingGun(block, text);
							}
						}
					};

				}
				else if (a.Id == "Shoot_Off")
				{
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
								if (Entity is IMyLargeTurretBase)
								{
									WeaponControlLayer.TerminalShootOffActionTurretBase.Invoke(block);
								}
								else
								{
									WeaponControlLayer.TerminalShootOffActionGatlingGun.Invoke(block);
								}
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
							if (Entity is IMyLargeTurretBase)
							{
								WeaponControlLayer.TerminalShootOffWriterTurretBase(block, text);
							}
							else
							{
								WeaponControlLayer.TerminalShootOffWriterGatlingGun(block, text);
							}
						}
					};
				}
			}

			List<IMyTerminalControl> controls = new List<IMyTerminalControl>();
			if (Entity is IMyLargeTurretBase)
			{
				MyAPIGateway.TerminalControls.GetControls<IMyLargeTurretBase>(out controls);
			}
			else if (Entity is IMySmallGatlingGun)
			{
				MyAPIGateway.TerminalControls.GetControls<IMySmallGatlingGun>(out controls);
			}

			foreach (IMyTerminalControl c in controls)
			{
				if (c.Id == "Shoot")
				{
					IMyTerminalControlOnOffSwitch onoff = c as IMyTerminalControlOnOffSwitch;

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
								if (Entity is IMyLargeTurretBase)
								{
									WeaponControlLayer.TerminalShootSetterTurretBase.Invoke(block, value);
								}
								else
								{
									WeaponControlLayer.TerminalShootSetterGatlingGun.Invoke(block, value);
								}
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
								if (Entity is IMyLargeTurretBase)
								{
									return WeaponControlLayer.TerminalShootGetterTurretBase.Invoke(block);
								}
								else
								{
									return WeaponControlLayer.TerminalShootGetterGatlingGun.Invoke(block);
								}
							}
						}
						catch (Exception e)
						{
							Tools.Warning($"Failed to get the Shoot On/Off terminal control\n {e}");
							return false;
						}
					};
				}
			}
		}

		protected void WeaponsFiringWriter(WeaponBase wb, StringBuilder str)
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
