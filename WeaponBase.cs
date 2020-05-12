using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using SENetworkAPI;
using VRage.Game;
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

		public NetSync<bool> TerminalShootOnce;
		public NetSync<bool> TerminalShooting;
		protected NetSync<float> CurrentReloadTime;
		protected NetSync<int> DeviationIndex;

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

		protected MyParticleEffect muzzleFlash;

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
			DeviationIndex = new NetSync<int>(ControlLayer, TransferType.ServerToClient, Tools.Random.Next(0, 128));

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
			Tools.Debug($"Restarting Weapon Logic {Entity.EntityId}");

			MyWeaponBlockDefinition blockDef = ((Entity as IMyFunctionalBlock).SlimBlock.BlockDefinition as MyWeaponBlockDefinition);
			WeaponDefinition d = Settings.WeaponDefinitionLookup[blockDef.WeaponDefinitionId.SubtypeId.String];
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
			if (!MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session != null)
			{
				//MyAPIGateway.Utilities.ShowNotification($"{(IsShooting ? "Shooting" : "Idle")}, RoF: {AmmoData.RateOfFire}, Shots: {CurrentShotInBurst}/{AmmoData.ShotsInBurst}, {(CurrentReloadTime.Value > 0 ? $"Cooldown {(ReloadTime - CurrentReloadTime.Value).ToString("n0")}/{ReloadTime}, " : "")}release: {CurrentReleaseTime.ToString("n0")}/{ReleaseTimeAfterFire}, Time: {TimeTillNextShot.ToString("n2")}", 1);
			}

			// true until proven false
			WillFireThisFrame = true;

			// If cooldown is greater than 0 the gun is on cooldown and should not fire
			// reduce cooldown and dont fire projectiles
			if (CurrentReloadTime.Value > 0)
			{
				CurrentReloadTime.SetValue(CurrentReloadTime.Value - Tools.MillisecondPerFrame, SyncType.None);
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
			if (!MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session != null)
			{
				//MyAPIGateway.Utilities.ShowNotification($"Deviation Index: {DeviationIndex.Value} MFTime: {(MuzzleFlashLifeSpan/1000f).ToString("n2")} MFCurrent: {muzzleFlash?.GetElapsedTime().ToString("n2")}", 1);
			}

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
				Vector3 direction = muzzleMatrix.Forward;
				Vector3D origin = muzzleMatrix.Translation;
				string ammoId = gun.GunBase.CurrentAmmoDefinition.Id.SubtypeId.String;
				AmmoDefinition ammo = Settings.AmmoDefinitionLookup[ammoId];

				while (TimeTillNextShot >= 1)
				{
					// calculate deviation
					int index = DeviationIndex.Value;
					MatrixD positionMatrix = Matrix.CreateWorld(origin, Tools.ApplyDeviation(direction, DeviateShotAngle, ref index), muzzleMatrix.Up);
					DeviationIndex.SetValue(index, SyncType.None);

					// spawn projectile
					Projectile bullet = new Projectile(Entity.EntityId, positionMatrix.Translation, positionMatrix.Forward, Block.CubeGrid.Physics.LinearVelocity, ammoId);
					Tools.Debug($"{Entity.EntityId} Deviation Index: {DeviationIndex.Value}");
					Core.SpawnProjectile(bullet);
					gun.GunBase.ConsumeAmmo();
					TimeTillNextShot--;

					// apply knock back
					if (ammo.BackkickForce > 0)
					{
						var forceVector = -direction * ammo.BackkickForce;
						Block.CubeGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, forceVector, Block.GetPosition(), Vector3.Zero);
					}

					// create sound
					//MakeShootSound();
					//MakeSecondaryShotSound();

					// create muzzle flash
					if (!MyAPIGateway.Utilities.IsDedicated /*Settings.Static.DrawMuzzleFlash*/)
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

			if (!MyAPIGateway.Utilities.IsDedicated /*Settings.Static.DrawMuzzleFlash*/)
			{
				if (muzzleFlash != null)
				{

					MatrixD muzzleMatrix = gun.GunBase.GetMuzzleWorldMatrix();
					Vector3 direction = muzzleMatrix.Forward;
					Vector3D origin = muzzleMatrix.Translation;

					MatrixD matrix = MatrixD.CreateFromDir(direction);
					matrix.Translation = origin;
					muzzleFlash.WorldMatrix = matrix;

					if (muzzleFlash.GetElapsedTime() > MuzzleFlashLifeSpan / 1000f)
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
