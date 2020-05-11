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

		public bool IsFixedGunTerminalShoot => (IsFixedGun && !((Entity.NeedsUpdate & MyEntityUpdateEnum.EACH_FRAME) == MyEntityUpdateEnum.EACH_FRAME));
		public bool IsShooting => gun.IsShooting || TerminalShootOnce.Value || TerminalShooting.Value || (IsFixedGun && (Entity.NeedsUpdate & MyEntityUpdateEnum.EACH_FRAME) == MyEntityUpdateEnum.EACH_FRAME);

		public NetSync<bool> TerminalShootOnce { get; private set; }
		public NetSync<bool> TerminalShooting { get; private set; }
		protected NetSync<float> CurrentReloadTime;


		protected bool WillFireThisFrame;
		protected int CurrentShotInBurst = 0;
		protected float CurrentReleaseTime = 0;
		protected double TimeTillNextShot = 1d;
		protected float CurrentIdleReloadTime = 0;


		protected WeaponControlLayer ControlLayer;
		protected MyEntity Entity;
		protected IMyFunctionalBlock Block;
		protected IMyCubeBlock Cube;
		protected IMyGunObject<MyGunBase> gun;

		/// <summary>
		/// Called when game logic is added to container
		/// </summary>
		public virtual void Init(WeaponControlLayer layer)
		{
			ControlLayer = layer;
			Entity = (MyEntity)layer.Entity;
			Block = Entity as IMyFunctionalBlock;
			Cube = Entity as IMyCubeBlock;
			gun = Entity as IMyGunObject<MyGunBase>;
			IsFixedGun = Entity is IMySmallGatlingGun;

			TerminalShootOnce = new NetSync<bool>(ControlLayer, TransferType.Both, false);
			TerminalShooting = new NetSync<bool>(ControlLayer, TransferType.Both, false);
			CurrentReloadTime = new NetSync<float>(ControlLayer, TransferType.ServerToClient, 0);
			CurrentReloadTime.ValueChangedByNetwork += CurrentReloadTimeUpdate;

			Initialized = true;
		}

		/// <summary>
		/// Called before update loop begins
		/// </summary>
		public virtual void Start() 
		{ 
		}

		/// <summary>
		/// Updates the definition when syncing to the server
		/// </summary>
		public virtual void SystemRestart()
		{
			WeaponDefinition d = Settings.WeaponDefinitionLookup[((Entity as IMyFunctionalBlock).SlimBlock.BlockDefinition as MyWeaponBlockDefinition).WeaponDefinitionId.SubtypeId.String];
			Copy(d);

			WillFireThisFrame = false;

			CurrentShotInBurst = 0;
			CurrentReloadTime.SetValue(0, SyncType.None);
			CurrentReleaseTime = 0;
			TimeTillNextShot = 1d;
			CurrentIdleReloadTime = 0;
		}

		/// <summary>
		/// First call in the update loop
		/// Used to update the firing state of this weapon
		/// </summary>
		public virtual void Update()
		{
			//if (!MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session != null)
			//{
			//	MyAPIGateway.Utilities.ShowNotification($"{(IsShooting ? "Shooting" : "Idle")}, RoF: {AmmoData.RateOfFire}, Shots: {CurrentShotInBurst}/{AmmoData.ShotsInBurst}, {(CurrentReloadTime.Value > 0 ? $"Cooldown {(ReloadTime - CurrentReloadTime.Value).ToString("n0")}/{ReloadTime}, " : "")}release: {CurrentReleaseTime.ToString("n0")}/{ReleaseTimeAfterFire}, Time: {TimeTillNextShot.ToString("n2")}", 1);
			//}

			// true until proven false
			WillFireThisFrame = true;

			// If cooldown is greater than 0 the gun is on cooldown and should not fire
			// reduce cooldown and dont fire projectiles
			if (CurrentReloadTime.Value > 0)
			{
				CurrentReloadTime.SetValue(CurrentReloadTime.Value-Tools.MillisecondPerFrame, SyncType.None);
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
		}

		/// <summary>
		/// Second call in the update loop
		/// Handles spawning projectiles
		/// </summary>
		public virtual void Spawn()
		{
			if (TimeTillNextShot >= 1 && WillFireThisFrame)
			{
				// Fixed guns do not update unless the mouse is pressed.
				// This updates the position when terminal fire is active.
				if (IsFixedGunTerminalShoot)
				{
					MyEntitySubpart subpart;
					if (Entity.Subparts.TryGetValue("Barrel", out subpart))
					{
						gun.GunBase.WorldMatrix = subpart.PositionComp.WorldMatrixRef;
					}
				}

				MatrixD muzzleMatrix = gun.GunBase.GetMuzzleWorldMatrix();

				string ammoId = gun.GunBase.CurrentAmmoDefinition.Id.SubtypeId.String;
				AmmoDefinition ammo = Settings.AmmoDefinitionLookup[ammoId];

				while (TimeTillNextShot >= 1)
				{
					MatrixD positionMatrix = muzzleMatrix;
					//MatrixD positionMatrix = Matrix.CreateWorld(
					//	muzzleMatrix.Translation,
					//	Randomizer.ApplyDeviation(Entity, muzzleMatrix.Forward, DeviateShotAngle),
					//	muzzleMatrix.Up);

					Projectile bullet = new Projectile(Entity.EntityId, positionMatrix.Translation, positionMatrix.Forward, Block.CubeGrid.Physics.LinearVelocity, ammoId);
					Core.SpawnProjectile(bullet);
					gun.GunBase.ConsumeAmmo();
					TimeTillNextShot--;
					//MakeShootSound();
					//MakeSecondaryShotSound();


					CurrentShotInBurst++;
					if (AmmoData.ShotsInBurst == 0)
					{
						CurrentShotInBurst = 0;
					}
					else if (CurrentShotInBurst == AmmoData.ShotsInBurst)
					{
						TimeTillNextShot = 0;
						CurrentShotInBurst = 0;
						CurrentReloadTime.Value = ReloadTime;
						break;
					}

					var forceVector = -positionMatrix.Forward * ammo.BackkickForce;
					Block.CubeGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, forceVector, positionMatrix.Translation, null);

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

		/// <summary>
		/// Third call in the update loop
		/// Handles animating small gatling gun barrel animations
		/// </summary>
		public virtual void Animate() 
		{ 
		}

		/// <summary>
		/// called on game logic closed
		/// </summary>
		public virtual void Close() 
		{
			CurrentReloadTime.ValueChangedByNetwork -= CurrentReloadTimeUpdate;
		}

		protected virtual void IdleReload()
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

		protected void CurrentReloadTimeUpdate(float o, float n, ulong steamId)
		{
			if (n == ReloadTime)
			{
				float delta = NetworkAPI.GetDeltaMilliseconds(CurrentReloadTime.LastMessageTimestamp);
				CurrentReloadTime.SetValue(CurrentReloadTime.Value - delta, SyncType.None);
				Tools.Debug($"ReloadTime: {delta}ms latency adjustment");
			}
		}

	}
}
