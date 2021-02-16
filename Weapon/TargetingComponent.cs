using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.Components;

namespace WeaponsOverhaul
{
	public class TargetingComponent : MyComponentBase
	{
		private MyCubeGrid Grid;

		public static TargetingComponent GetOrAddComponent(MyCubeGrid grid)
		{
			TargetingComponent gggc = grid.Components.Get<TargetingComponent>();

			if (gggc == null)
			{
				gggc = new TargetingComponent();
				gggc.Init(grid);
				grid.Components.Add(gggc);
			}

			return gggc;
		}


		public void Init(MyCubeGrid grid)
		{
			Grid = grid;
		}
	}
}
