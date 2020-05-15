using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using SENetworkAPI;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Components;
using VRage.Input;
using VRage.Library.Utils;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using WeaponsOverhaul.Definitions;

namespace WeaponsOverhaul
{
	public class WeaponBase : WeaponDefinition
	{
		public bool Initialized { get; private set; } = false;
		public bool IsFixedGun { get; private set; }
		public bool IsShooting => gun.IsShooting || TerminalShootOnce.Value || TerminalShooting.Value || ManualShooting.Value;
		public bool IsOutOfAmmo => !gun.GunBase.HasEnoughAmmunition();
		public bool IsReloading => CurrentReloadTime.Value > 0;


		public NetSync<bool> ManualShooting;
		public NetSync<bool> TerminalShootOnce;
		public NetSync<bool> TerminalShooting;

		protected NetSync<float> CurrentReloadTime;
		protected NetSync<sbyte> DeviationIndex;

		protected bool WillFireThisFrame;
		protected int CurrentShotInBurst = 0;
		protected float CurrentReleaseTime = 0;
		protected double TimeTillNextShot = 1d;
		protected float CurrentIdleReloadTime = 0;

		protected bool FirstTime;


		protected WeaponControlLayer ControlLayer;
		protected MyEntity Entity;
		protected IMyFunctionalBlock Block;
		protected IMyGunObject<MyGunBase> gun;

		protected MyParticleEffect muzzleFlash;

		protected MyEntity3DSoundEmitter PrimaryEmitter;
		protected MyEntity3DSoundEmitter SecondaryEmitter;

		//protected static SerializableDefinitionId SelectedWeapon;
		protected static MyDefinitionId WeaponDefinition;

		/// <summary>
		/// Called when game logic is added to container
		/// </summary>
		public virtual void Init(WeaponControlLayer layer)
		{
			ControlLayer = layer;
			Entity = (MyEntity)layer.Entity;
			Block = Entity as IMyFunctionalBlock;
			gun = Entity as IMyGunObject<MyGunBase>;
			IsFixedGun = Entity is IMySmallGatlingGun;

			ManualShooting = new NetSync<bool>(ControlLayer, TransferType.Both, false);
			TerminalShootOnce = new NetSync<bool>(ControlLayer, TransferType.Both, false);
			TerminalShooting = new NetSync<bool>(ControlLayer, TransferType.Both, false);
			CurrentReloadTime = new NetSync<float>(ControlLayer, TransferType.ServerToClient, 0);
			CurrentReloadTime.ValueChangedByNetwork += CurrentReloadTimeUpdate;
			DeviationIndex = new NetSync<sbyte>(ControlLayer, TransferType.ServerToClient, (sbyte)MyRandom.Instance.Next(0, sbyte.MaxValue));

			PrimaryEmitter = new MyEntity3DSoundEmitter(Entity, useStaticList: true);
			SecondaryEmitter = new MyEntity3DSoundEmitter(Entity, useStaticList: true);
			InitializeSound();

			Initialized = true;
			FirstTime = true;
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

		/// <summary>
		/// Called before update loop begins
		/// </summary>
		public virtual void Start()
		{
			WeaponDefinition = Block.SlimBlock.BlockDefinition.Id;
		}

		/// <summary>
		/// Updates the definition when syncing to the server
		/// </summary>
		public virtual void SystemRestart()
		{
			//Tools.Debug($"Restarting Weapon Logic {Entity.EntityId}");

			MyWeaponBlockDefinition blockDef = ((Entity as IMyFunctionalBlock).SlimBlock.BlockDefinition as MyWeaponBlockDefinition);
			WeaponDefinition d = Settings.WeaponDefinitionLookup[blockDef.WeaponDefinitionId.SubtypeId.String];
			Copy(d);

			WillFireThisFrame = false;

			CurrentShotInBurst = 0;
			CurrentReloadTime.SetValue(0, SyncType.None);
			CurrentReleaseTime = 0;
			TimeTillNextShot = 1d;
			CurrentIdleReloadTime = 0;
			InitializeSound();
		}

		protected void HandleInputs()
		{
			if (Block.CubeGrid.EntityId != Notifications.ControlledGridId || Notifications.SelectedDefinition != WeaponDefinition)
				return;

			if (MyAPIGateway.Gui.IsCursorVisible)
				return;

			if (MyAPIGateway.Input.IsNewPrimaryButtonPressed())
			{
				ManualShooting.Value = true;
			}

			if (MyAPIGateway.Input.IsNewPrimaryButtonReleased())
			{
				ManualShooting.Value = false;
			}
		}

		/// <summary>
		/// First call in the update loop
		/// Used to update the firing state of this weapon
		/// </summary>
		public virtual void Update()
		{
			HandleInputs();

			if (!IsShooting && (TimeTillNextShot == 1 || !Block.IsWorking))
				return;
			
			//if (!MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session != null)
			//{
			//	MyAPIGateway.Utilities.ShowNotification($"{(IsShooting ? "Shooting" : "Idle")}, RoF: {AmmoData.RateOfFire}, Shots: {CurrentShotInBurst}/{AmmoData.ShotsInBurst}, {(CurrentReloadTime.Value > 0 ? $"Cooldown {(ReloadTime - CurrentReloadTime.Value).ToString("n0")}/{ReloadTime}, " : "")}release: {CurrentReleaseTime.ToString("n0")}/{ReleaseTimeAfterFire}, Time: {TimeTillNextShot.ToString("n2")}", 1);
			//}

			// true until proven false
			WillFireThisFrame = true;

			// This is to stop weapons from firing on place
			//if (FirstTime)
			//{
			//	WillFireThisFrame = false;
			//	FirstTime = false;
			//	return;
			//}

			// If cooldown is greater than 0 the gun is on cooldown and should not fire
			// reduce cooldown and dont fire projectiles
			if (CurrentReloadTime.Value > 0)
			{
				CurrentReloadTime.SetValue(CurrentReloadTime.Value - Tools.MillisecondPerFrame, SyncType.None);
				WillFireThisFrame = false;
			}

			// if the block is not functional toggle shooting to off
			// this is not venilla and may get changed
			if (!Block.IsWorking)
			{
				TerminalShooting.SetValue(false, SyncType.None);
				WillFireThisFrame = false;
				return;
			}

			// if a user is manually shooting toggle terminal shoot off
			if (gun.IsShooting)
			{
				TerminalShooting.SetValue(false, SyncType.None);
			}

			// do not shoot in safe zone
			if (WillFireThisFrame && !MySessionComponentSafeZones.IsActionAllowed(Block.GetPosition(), Tools.CastProhibit(MySessionComponentSafeZones.AllowedActions, 2)))
			{
				TerminalShooting.SetValue(false, SyncType.None);
				TerminalShootOnce.SetValue(false, SyncType.None);
				WillFireThisFrame = false;
			}

			// this makes sure the gun will fire instantly when fire condisions are met
			if (!IsShooting ||
				Block?.CubeGrid?.Physics == null ||
				!gun.GunBase.HasEnoughAmmunition() ||
				!WillFireThisFrame)
			{

				if (TimeTillNextShot < 1)
				{
					TimeTillNextShot += AmmoData.RateOfFire * Tools.FireRateMultiplayer;
				}

				if (TimeTillNextShot > 1)
				{
					TimeTillNextShot = 1;
				}

				WillFireThisFrame = false;
			}

			if (WillFireThisFrame)
			{
				TimeTillNextShot += AmmoData.RateOfFire * Tools.FireRateMultiplayer;
			}

			if (IsShooting && !gun.GunBase.HasEnoughAmmunition())
			{
				StartNoAmmoSound();
				Notifications.Display(Block.CubeGrid.EntityId);
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
			//if (!MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session != null)
			//{
			//	MyAPIGateway.Utilities.ShowNotification($"Deviation Index: {DeviationIndex.Value} MFTime: {(MuzzleFlashLifeSpan/1000f).ToString("n2")} MFCurrent: {muzzleFlash?.GetElapsedTime().ToString("n2")}", 1);
			//}

			if (TimeTillNextShot >= 1 && WillFireThisFrame)
			{
				// Fixed guns do not update unless the mouse is pressed.
				// This updates the position when terminal fire is active.
				if (IsFixedGun)
				{
					MyEntitySubpart subpart;
					if (Entity.Subparts.TryGetValue("Barrel", out subpart))
					{
						gun.GunBase.WorldMatrix = subpart.PositionComp.WorldMatrixRef;
					}
				}

				MatrixD muzzleMatrix = gun.GunBase.GetMuzzleWorldMatrix();
				Vector3 direction = muzzleMatrix.Forward;
				Vector3D origin = muzzleMatrix.Translation;
				string ammoId = gun.GunBase.CurrentAmmoDefinition.Id.SubtypeId.String;
				AmmoDefinition ammo = Settings.AmmoDefinitionLookup[ammoId];

				while (TimeTillNextShot >= 1)
				{
					// calculate deviation
					sbyte index = DeviationIndex.Value;
					MatrixD positionMatrix = Matrix.CreateWorld(origin, Tools.ApplyDeviation(direction, DeviateShotAngle, ref index), muzzleMatrix.Up);
					DeviationIndex.SetValue(index, SyncType.None);

					// spawn projectile
					Projectile bullet = new Projectile(Entity.EntityId, positionMatrix.Translation, positionMatrix.Forward, Block.CubeGrid.Physics.LinearVelocity, ammoId);
					Core.SpawnProjectile(bullet);
					gun.GunBase.ConsumeAmmo();
					TimeTillNextShot--;

					//apply recoil
					if (ammo.BackkickForce > 0)
					{
						Core.PhysicsRequests.Enqueue(new PhysicsDefinition {
							Target = Entity,
							Force = -direction * ammo.BackkickForce,
							Position = Entity.WorldMatrix.Translation
						});
					}

					// create sound
					StartShootSound();
					//MakeSecondaryShotSound();

					// create muzzle flash
					if (!MyAPIGateway.Utilities.IsDedicated && Settings.Static.DrawMuzzleFlash)
					{
						MatrixD matrix = MatrixD.CreateFromDir(direction);
						matrix.Translation = origin;
						if (muzzleFlash == null || muzzleFlash.IsStopped)
						{
							bool foundParticle = MyParticlesManager.TryCreateParticleEffect(MuzzleFlashSpriteName, ref matrix, ref origin, uint.MaxValue, out muzzleFlash);
							if (foundParticle)
							{
								muzzleFlash.Play();
							}
						}
					}

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
						DeviationIndex.Push();
						TerminalShootOnce.SetValue(false, SyncType.None);
						break;
					}

					if (TerminalShootOnce.Value)
					{
						TerminalShootOnce.SetValue(false, SyncType.None);
						return;
					}
				}
			}
		}

		public void StartShootSound()
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

		public void StartNoAmmoSound()
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

			if (!MyAPIGateway.Utilities.IsDedicated && Settings.Static.DrawMuzzleFlash)
			{
				if (muzzleFlash != null)
				{
					MatrixD muzzleMatrix = gun.GunBase.GetMuzzleWorldMatrix();
					Vector3 direction = muzzleMatrix.Forward;
					Vector3D origin = muzzleMatrix.Translation;

					MatrixD matrix = MatrixD.CreateFromDir(direction);
					matrix.Translation = origin;
					muzzleFlash.WorldMatrix = matrix;

					if (muzzleFlash.GetElapsedTime() > MuzzleFlashLifeSpan * 0.001f)
					{
						muzzleFlash.Stop();
					}
				}
			}
		}

		/// <summary>
		/// called on game logic closed
		/// </summary>
		public virtual void Close()
		{
			muzzleFlash?.Stop();
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
				//Tools.Debug($"ReloadTime: {delta}ms latency adjustment");
			}
		}

	}
}
