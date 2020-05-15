using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SENetworkAPI;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;

namespace WeaponsOverhaul
{
	[MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
	public class Notifications : MyNetworkSessionComponent
	{
		private int Tick;
		private int Reloading;
		private int OutOfAmmo;
		private int NonFunctional;
		private int Off;

		private static bool DisplayAnyway;
		private static long ControlledGridId;

		public static void Display(long gridId) 
		{
			if (ControlledGridId == gridId)
			{
				DisplayAnyway = true;
			}
		}

		private List<WeaponControlLayer> GridWeapons = new List<WeaponControlLayer>();

		public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
		{
			if (MyAPIGateway.Utilities.IsDedicated)
				return;

			SetUpdateOrder(MyUpdateOrder.BeforeSimulation);
		}

		public override void BeforeStart()
		{
			MyAPIGateway.Session.LocalHumanPlayer.Controller.ControlledEntityChanged += Changed;
		}

		private void Changed(VRage.Game.ModAPI.Interfaces.IMyControllableEntity o, VRage.Game.ModAPI.Interfaces.IMyControllableEntity n)
		{
			GridWeapons.Clear();
			ControlledGridId = 0;
			Tick = 179;
			Reloading = 0;
			OutOfAmmo = 0;
			NonFunctional = 0;
			Off = 0;

			MyCubeGrid grid = (n?.Entity as MyCubeBlock)?.CubeGrid; // is controlling a turret
			if (grid == null)
			{
				grid = n?.Entity as MyCubeGrid; // is controlling a grid
			}

			if (grid != null)
			{
				ControlledGridId = grid.EntityId;
				foreach (MyCubeBlock block in grid.GetFatBlocks())
				{
					WeaponControlLayer layer = block.GameLogic.GetAs<WeaponControlLayer>();

					if (layer != null)
					{
						GridWeapons.Add(layer);
					}
				}
			}
		}

		public override void UpdateBeforeSimulation()
		{
			Tick++;
			if (Tick == 180)
			{
				Tick = 1;

				int reloading = 0;
				int outOfAmmo = 0;
				int nonFunctional = 0;
				int off = 0;

				foreach (WeaponControlLayer layer in GridWeapons)
				{
					if (layer == null)
					{
						nonFunctional++;
					}
					else
					{
						IMyFunctionalBlock f = (layer.Entity as IMyFunctionalBlock);
						if (f == null)
						{
							nonFunctional++;
							continue;
						}

						if (!f.IsFunctional)
							nonFunctional++;
						if (!f.IsWorking)
							off++;
						if (layer.Weapon.IsOutOfAmmo)
							outOfAmmo++;
						if (layer.Weapon.IsReloading)
							reloading++;
					}
				}

				if (DisplayAnyway || Reloading != reloading || OutOfAmmo != outOfAmmo || NonFunctional != nonFunctional || Off != off)
				{
					MyAPIGateway.Utilities.ShowNotification($"Weapons - off ({off}) damaged ({nonFunctional}) reloading ({reloading}) - out ({outOfAmmo})", 3000, "Red");

					Reloading = reloading;
					OutOfAmmo = outOfAmmo;
					NonFunctional = nonFunctional;
					Off = off;
					DisplayAnyway = false;
				}
			}
		}
	}
}
