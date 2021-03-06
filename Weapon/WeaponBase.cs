﻿using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using SENetworkAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Library.Utils;
using VRageMath;

namespace WeaponsOverhaul
{
	public enum WeaponState { None = 0, /*Reloading = 1,*/ ManualShoot = 2, AIShoot = 4, TerminalShoot = 8, TerminalShootOnce = 16 }

	public class WeaponBase : WeaponDefinition, IWeapon
	{
		public bool Initialized { get; private set; } = false;
		public bool IsFixedGun { get; private set; }

		//gun.IsShooting || TerminalShootOnce.Value || TerminalShooting.Value || ManualShooting.Value;
		public bool IsShooting => (State.Value & WeaponState.AIShoot) == WeaponState.AIShoot ||
						(State.Value & WeaponState.ManualShoot) == WeaponState.ManualShoot ||
						(State.Value & WeaponState.TerminalShoot) == WeaponState.TerminalShoot ||
						(State.Value & WeaponState.TerminalShootOnce) == WeaponState.TerminalShootOnce;

		public bool IsAnimated => MuzzleFlashActive; //TODO: add barrel rotation at some point
		public bool IsOutOfAmmo => !gun.GunBase.HasEnoughAmmunition();
		public bool IsReloading => Reloading.Value;//(State.Value & WeaponState.Reloading) == WeaponState.Reloading;
		public bool IsAIShooting 
		{
			get { return (State.Value & WeaponState.AIShoot) == WeaponState.AIShoot; }
			set 
			{
				if (value != ((State.Value & WeaponState.AIShoot) == WeaponState.AIShoot))
				{
					if (value)
					{
						State.Value = WeaponState.AIShoot;
					}
					else
					{
						State.Value &= ~WeaponState.AIShoot;
					}
				}	
			}
		} 
		public bool IsManualShooting 
		{
			get { return (State.Value & WeaponState.ManualShoot) == WeaponState.ManualShoot; }
			set 
			{
				if (value != ((State.Value & WeaponState.ManualShoot) == WeaponState.ManualShoot))
				{
					if (value)
					{
						State.Value = WeaponState.ManualShoot;
					}
					else
					{
						State.Value &= ~WeaponState.ManualShoot;
					}
				}
			}
		}
		public bool IsTerminalShooting
		{
			get { return (State.Value & WeaponState.TerminalShoot) == WeaponState.TerminalShoot; }
			set
			{
				if (value != ((State.Value & WeaponState.TerminalShoot) == WeaponState.TerminalShoot))
				{
					if (value)
					{
						State.Value |= WeaponState.TerminalShoot;
					}
					else
					{
						State.Value &= ~WeaponState.TerminalShoot;
					}
				}
			}
		}

		public bool IsTerminalShootOnce 
		{
			get { return (State.Value & WeaponState.TerminalShootOnce) == WeaponState.TerminalShootOnce; }
			set
			{
				if (value != ((State.Value & WeaponState.TerminalShootOnce) == WeaponState.TerminalShootOnce))
				{
					if (value)
					{
						State.Value |= WeaponState.TerminalShootOnce;
					}
					else
					{
						State.Value &= ~WeaponState.TerminalShootOnce;
					}
				}
			}
		}

		public NetSync<WeaponState> State;
		protected NetSync<bool> Reloading;
		protected NetSync<sbyte> DeviationIndex;

		protected int CurrentShotInBurst = 0;
		protected float CurrentIdleReloadTime = 0;
		protected DateTime LastShootTime;
		protected DateTime LastAmmoFeed;
		protected MyInventory Inventory;
		protected int AmmoFeedInterval = 5000;

		protected bool MuzzleFlashActive;
		protected float MuzzleFlashCurrentTime;

		public WeaponControlLayer ControlLayer { get; private set; }
		public MyCubeBlock CubeBlock { get; private set; }
		public IMyFunctionalBlock Block { get; private set; }
		public IMyGunObject<MyGunBase> gun { get; private set; }

		protected MyParticleEffect muzzleFlash;
		protected MyEntity3DSoundEmitter PrimaryEmitter;
		protected MyEntity3DSoundEmitter SecondaryEmitter;

		//protected static SerializableDefinitionId SelectedWeapon;
		public MyDefinitionId WeaponDefinition;

		protected byte Notify = 0x0;


		/// <summary>
		/// Called when game logic is added to container
		/// </summary>
		public virtual void Init(WeaponControlLayer layer)
		{
			ControlLayer = layer;
			CubeBlock = (MyCubeBlock)layer.Entity;
			//Targeting = ;
			Block = CubeBlock as IMyFunctionalBlock;
			gun = CubeBlock as IMyGunObject<MyGunBase>;
			IsFixedGun = CubeBlock is IMySmallGatlingGun;

			PrimaryEmitter = new MyEntity3DSoundEmitter(CubeBlock, useStaticList: true);
			SecondaryEmitter = new MyEntity3DSoundEmitter(CubeBlock, useStaticList: true);
			InitializeSound();

			Initialized = true;
		}

		private void InitializeSound()
		{
			if (!string.IsNullOrWhiteSpace(NoAmmoSound))
			{
				NoAmmoSoundPair = new MySoundPair(NoAmmoSound);
			}

			if (!string.IsNullOrWhiteSpace(SecondarySound))
			{
				SecondarySoundPair = new MySoundPair(SecondarySound);
			}

			if (!string.IsNullOrWhiteSpace(ReloadSound))
			{
				ReloadSoundPair = new MySoundPair(ReloadSound);
			}

			if (AmmoData != null && !string.IsNullOrWhiteSpace(AmmoData.ShootSound))
			{
				AmmoData.ShootSoundPair = new MySoundPair(AmmoData.ShootSound);
			}
		}

		private void StateChanged(WeaponState o, WeaponState n)
		{
			//try
			//{
			//	bool shooting = IsShooting;
			//	bool oldshoot = ((o & WeaponState.AIShoot) == WeaponState.AIShoot ||
			//		(o & WeaponState.ManualShoot) == WeaponState.ManualShoot ||
			//		(o & WeaponState.TerminalShoot) == WeaponState.TerminalShoot ||
			//		(o & WeaponState.TerminalShootOnce) == WeaponState.TerminalShootOnce);

			//	if (oldshoot != shooting)
			//	{
			//		if (shooting)
			//		{
			//			ControlLayer.NeedsUpdate = VRage.ModAPI.MyEntityUpdateEnum.EACH_FRAME;
			//		}
			//		else if (!IsAnimated)
			//		{
			//			ControlLayer.NeedsUpdate = VRage.ModAPI.MyEntityUpdateEnum.NONE;
			//		}
			//	}
			//}
			//catch (Exception e)
			//{
			//	Tools.Error(e.ToString());
			//}
		}

		/// <summary>
		/// Called before update loop begins
		/// </summary>
		public virtual void Start()
		{
			WeaponDefinition = CubeBlock.BlockDefinition.Id;

			State = new NetSync<WeaponState>(ControlLayer, TransferType.Both, WeaponState.None);
			State.ValueChanged += StateChanged;
			Reloading = new NetSync<bool>(ControlLayer, TransferType.ServerToClient, false);
			DeviationIndex = new NetSync<sbyte>(ControlLayer, TransferType.ServerToClient, (sbyte)MyRandom.Instance.Next(0, sbyte.MaxValue));
			InventoryComponent.GetOrAddComponent(CubeBlock.CubeGrid);
			Inventory = CubeBlock.GetInventory();
		}

		/// <summary>
		/// Updates the definition when syncing to the server
		/// </summary>
		public virtual void SystemRestart()
		{
			//Tools.Debug($"Restarting Weapon Logic {Entity.EntityId}");

			MyWeaponBlockDefinition blockDef = ((CubeBlock as IMyFunctionalBlock).SlimBlock.BlockDefinition as MyWeaponBlockDefinition);
			WeaponDefinition d = Settings.WeaponDefinitionLookup[blockDef.WeaponDefinitionId.SubtypeId.String];
			Copy(d);

			CurrentShotInBurst = 0;
			CurrentIdleReloadTime = 0;
			InitializeSound();
		}

		int count = 0;
		/// <summary>
		/// First call in the update loop
		/// Used to update the firing state of this weapon
		/// </summary>
		public virtual void Update()
		{
			// stop looping if not shooting
			if (!IsShooting)
			{
				//if (!IsAnimated)
				//{
				//	ControlLayer.NeedsUpdate = VRage.ModAPI.MyEntityUpdateEnum.NONE;
				//}

				return;
			}

			byte notify = 0x0;
			DateTime currentTime = DateTime.UtcNow;
			double timeSinceLastShot = (currentTime - LastShootTime).TotalMilliseconds;
			double timeSinceLastAmmoFeed = (currentTime - LastAmmoFeed).TotalMilliseconds;

			if (timeSinceLastAmmoFeed > AmmoFeedInterval)
			{
				if ((Inventory.CurrentVolume.RawValue / Inventory.MaxVolume.RawValue) < InventoryFillFactorMin)
				{
					InventoryComponent.Fill(CubeBlock, gun.GunBase.CurrentAmmoMagazineId);
				}
			}

			//if (!MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session != null)
			//{
			//	MyAPIGateway.Utilities.ShowNotification($"ShootTime: {timeSinceLastShot.ToString("n0")}ms - {State.Value} - {IsShooting} - {IsReloading} - {(timeSinceLastShot * (AmmoData.RateOfFire * Tools.MinutesToMilliseconds)).ToString("n2")} {CurrentShotInBurst}/{AmmoData.ShotsInBurst}", 1);
			//}

			// Stops if weapons are reloading
			if (IsReloading)
			{
				if (timeSinceLastShot < ReloadTime)
					return;

				Reloading.Value = false;
				//State.Value &= ~WeaponState.Reloading;
			}

			// Stops if weapons are not ready to fire this frame
			if (timeSinceLastShot * (AmmoData.RateOfFire * Tools.MinutesToMilliseconds) < 1f)
				return;


			// Stops if weapons are not working/functional
			if (!Block.IsWorking)
			{
				if (!Block.IsFunctional)
				{
					notify |= 0x1;
				}
				else
				{
					notify |= 0x2;
				}
			}
			else
			{

				bool enoughAmmo = gun.GunBase.HasEnoughAmmunition();
				if (!enoughAmmo)
				{
					StartNoAmmoSound();
					notify |= 0x4;
				}
				else if (MySessionComponentSafeZones.IsActionAllowed(Block.GetPosition(), Tools.CastProhibit(MySessionComponentSafeZones.AllowedActions, 2)))
				{
					// Fixed guns do not update unless the mouse is pressed.
					// This updates the position when terminal fire is active.
					if (IsFixedGun)
					{
						MyEntitySubpart subpart;
						if (CubeBlock.Subparts.TryGetValue("Barrel", out subpart))
						{
							gun.GunBase.WorldMatrix = subpart.PositionComp.WorldMatrixRef;
						}
					}

					// NOTE: RateOfFire is limited to 3600 rounds per seconds using this method

					MatrixD muzzleMatrix = gun.GunBase.GetMuzzleWorldMatrix();
					Vector3 direction = muzzleMatrix.Forward;
					Vector3D origin = muzzleMatrix.Translation;
					string ammoId = gun.GunBase.CurrentAmmoDefinition.Id.SubtypeId.String;
					AmmoDefinition ammo = Settings.AmmoDefinitionLookup[ammoId];

					// calculate deviation
					sbyte index = DeviationIndex.Value;
					MatrixD positionMatrix = Matrix.CreateWorld(origin, Tools.ApplyDeviation(direction, DeviateShotAngle, ref index), muzzleMatrix.Up);
					DeviationIndex.SetValue(index, SyncType.None);

					// spawn projectile
					Core.Static.Spawn(positionMatrix.Translation, positionMatrix.Forward, Block.CubeGrid.Physics.LinearVelocity, Block.EntityId, ammo);
					//Projectile bullet = new Projectile(CubeBlock.EntityId, positionMatrix.Translation, positionMatrix.Forward, Block.CubeGrid.Physics.LinearVelocity, ammoId);
					//Core.SpawnProjectile(bullet);
					gun.GunBase.ConsumeAmmo();

					//apply recoil
					if (ammo.BackkickForce > 0)
					{
						//CubeBlock.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, -direction * ammo.BackkickForce, CubeBlock.WorldMatrix.Translation, Vector3.Zero);

						//Core.PhysicsRequests.Enqueue(new PhysicsDefinition {
						//	Target = CubeBlock,
						//	Force = -direction * ammo.BackkickForce,
						//	Position = CubeBlock.WorldMatrix.Translation
						//});
					}

					// create sound
					StartShootSound();
					//MakeSecondaryShotSound();

					// create muzzle flash
					if (!MyAPIGateway.Utilities.IsDedicated && Settings.Static.DrawMuzzleFlash)
					{
						MatrixD matrix = MatrixD.CreateFromDir(direction);
						matrix.Translation = origin;

						bool foundParticle = MyParticlesManager.TryCreateParticleEffect(MuzzleFlashSpriteName, ref matrix, ref origin, uint.MaxValue, out muzzleFlash);
						if (foundParticle)
						{
							MuzzleFlashActive = true;
							MuzzleFlashCurrentTime = 0;
							muzzleFlash.Play();
						}
					}

					CurrentShotInBurst++;
					if (AmmoData.ShotsInBurst == 0)
					{
						CurrentShotInBurst = 0;
					}
					else if (CurrentShotInBurst == AmmoData.ShotsInBurst)
					{
						notify |= 0x8;
						CurrentShotInBurst = 0;
						Reloading.Value = true;
						//State.Value |= WeaponState.Reloading;
						DeviationIndex.Push();
					}

					if (IsTerminalShootOnce)
					{
						State.SetValue(State.Value & ~WeaponState.TerminalShootOnce);
					}

					LastShootTime = currentTime;
				}
			}

			if (Notify != notify)
			{
				Core.NotifyNextFrame(Block.CubeGrid.EntityId);
				Notify = notify;
			}
		}

		public virtual void Update100() 
		{
			Tools.Debug("Made it!");

			//if (inventory.CurrentVolume == 0)
			//{
			//	InventoryComponent.Fill(CubeBlock, gun.GunBase.CurrentAmmoMagazineId);
			//}
			//else if (IsShooting)
			//{

			//}
		}

		private void StartShootSound()
		{
			if (AmmoData.ShootSoundPair == null || PrimaryEmitter == null)
				return;

			if (PrimaryEmitter.IsPlaying)
			{
				if (!PrimaryEmitter.Loop)
				{
					PrimaryEmitter.PlaySound(AmmoData.ShootSoundPair, false, false, false);
				}
			}
			else
			{
				PrimaryEmitter.PlaySound(AmmoData.ShootSoundPair, true, false, false);
			}
		}

		private void StartNoAmmoSound()
		{
			if (NoAmmoSoundPair == null || PrimaryEmitter == null)
				return;

			if (!PrimaryEmitter.IsPlaying)
			{
				PrimaryEmitter.PlaySound(NoAmmoSoundPair, true, false, false);
			}
		}

		/// <summary>
		/// Third call in the update loop
		/// Handles animating small gatling gun barrel animations
		/// </summary>
		public virtual void Animate()
		{
			if (MyAPIGateway.Utilities.IsDedicated || !IsAnimated)
				return;

			if (Settings.Static.DrawMuzzleFlash && muzzleFlash != null)
			{
				MuzzleFlashCurrentTime += Tools.Tick;

				MatrixD muzzleMatrix = gun.GunBase.GetMuzzleWorldMatrix();
				Vector3 direction = muzzleMatrix.Forward;
				Vector3D origin = muzzleMatrix.Translation;

				MatrixD matrix = MatrixD.CreateFromDir(direction);
				matrix.Translation = origin;
				muzzleFlash.WorldMatrix = matrix;

				if (MuzzleFlashCurrentTime * 1000 > MuzzleFlashLifeSpan)
				{
					muzzleFlash.Stop(true);
					MuzzleFlashActive = false;
				}
			}
		}

		/// <summary>
		/// called on game logic closed
		/// </summary>
		public virtual void Close()
		{
			muzzleFlash?.Stop();
		}
	}
}
