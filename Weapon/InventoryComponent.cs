using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace WeaponsOverhaul
{
	public class InventoryComponent : MyComponentBase
	{
		private MyCubeGrid Grid;
		private HashSet<IMyInventory> Inventories = new HashSet<IMyInventory>();

		public static InventoryComponent GetOrAddComponent(MyCubeGrid grid)
		{
			InventoryComponent gggc = grid.Components.Get<InventoryComponent>();

			if (gggc == null)
			{
				gggc = new InventoryComponent();
				gggc.Init(grid);
				grid.Components.Add(gggc);
			}

			return gggc;
		}

		public void Init(MyCubeGrid grid) 
		{
			if (Grid != null) return;

			Grid = grid;

			foreach (MyCubeBlock block in grid.GetFatBlocks())
			{
				AddBlockInventory(block);
			}

			((IMyCubeGrid)Grid).OnBlockAdded += AddBlockInventory;
			((IMyCubeGrid)Grid).OnBlockRemoved += RemoveBlockInventory;


		}

		public static void Fill(MyCubeBlock block, MyDefinitionId itemId) 
		{
			
			if (!block.HasInventory)
				return;

			InventoryComponent comp = block.CubeGrid.Components.Get<InventoryComponent>();
			if (comp == null)
				return;

			MyInventory target = block.GetInventory(0);
			MyFixedPoint ammoNeeded = target.ComputeAmountThatFits(itemId);

			foreach (MyInventory source in comp.Inventories)
			{
				if (ammoNeeded == 0)
					return;

				var item = source.FindItem(itemId);

				if (!item.HasValue || item.Value.Amount == 0)
					continue;

				if (!((IMyInventory)source).IsConnectedTo(target))
					continue;

				int index = source.GetItemIndexById(item.Value.ItemId);
				if (item.Value.Amount > ammoNeeded)
				{
					target.TransferItemFrom(source, index, null, true, ammoNeeded);
					break;
				}
				else
				{
					target.TransferItemFrom(source, index, null, true, item.Value.Amount);
					ammoNeeded -= item.Value.Amount;
				}
			}

		}

		public override void OnBeforeRemovedFromContainer()
		{
			((IMyCubeGrid)Grid).OnBlockAdded -= AddBlockInventory;
			((IMyCubeGrid)Grid).OnBlockRemoved += RemoveBlockInventory;
		}

		private void AddBlockInventory(IMySlimBlock slim) 
		{
			if (slim.FatBlock != null)
			{
				AddBlockInventory(slim.FatBlock as MyCubeBlock);
			}
		}

		private void AddBlockInventory(MyCubeBlock block) 
		{
			if (block is IMyGunBaseUser)
				return;

			if (block.HasInventory)
			{
				IMyInventory inventory = block.GetInventory(0);
				if (inventory != null)
					Inventories.Add(inventory);
				
				inventory = block.GetInventory(1);
				if (inventory != null)
					Inventories.Add(inventory);

				inventory = block.GetInventory(2); // just adding this because
				if (inventory != null)
					Inventories.Add(inventory);
			}
		}


		private void RemoveBlockInventory(IMySlimBlock slim)
		{
			if (slim.FatBlock != null)
			{
				RemoveBlockInventory(slim.FatBlock as MyCubeBlock);
			}
		}

		private void RemoveBlockInventory(MyCubeBlock block) 
		{
			if (block is IMyGunBaseUser)
				return;

			if (block.HasInventory)
			{
				IMyInventory inventory = block.GetInventory(0);
				if (inventory != null)
					Inventories.Remove(inventory);

				inventory = block.GetInventory(1);
				if (inventory != null)
					Inventories.Remove(inventory);

				inventory = block.GetInventory(2); // just adding this because
				if (inventory != null)
					Inventories.Remove(inventory);
			}
		}
	}
}
