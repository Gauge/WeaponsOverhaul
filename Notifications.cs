using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SENetworkAPI;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Input;
using VRage.ObjectBuilders;

namespace WeaponsOverhaul
{
	[MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
	public class Notifications : MyNetworkSessionComponent
	{
		public static long ControlledGridId;
		public static SerializableDefinitionId SelectedDefinition;

		private static bool DisplayAnyway;

		private int Tick;
		private int Reloading;
		private int OutOfAmmo;
		private int NonFunctional;
		private int Off;

		private IMyShipController ActiveShipController;

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
			Changed(null, MyAPIGateway.Session.LocalHumanPlayer.Controller.ControlledEntity);
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

			ActiveShipController = n?.Entity as IMyShipController;
			SelectedDefinition = Tools.GetSelectedHotbarDefinition(ActiveShipController);
			MyCubeGrid grid = (n?.Entity as MyCubeBlock)?.CubeGrid; // is controlling a turret

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
			List<MyKeys> keys = new List<MyKeys>();
			MyAPIGateway.Input.GetPressedKeys(keys);

			foreach (var key in keys)
			{
				if (key == MyKeys.D1 ||
					key == MyKeys.D2 ||
					key == MyKeys.D3 ||
					key == MyKeys.D4 ||
					key == MyKeys.D5 ||
					key == MyKeys.D6 ||
					key == MyKeys.D7 ||
					key == MyKeys.D8 ||
					key == MyKeys.D9)
				{
					SelectedDefinition = Tools.GetSelectedHotbarDefinition(ActiveShipController);
				}
			}

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
