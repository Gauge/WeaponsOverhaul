using System;
using System.Collections.Generic;
using System.Text;

namespace WeaponsOverhaul.Definitions
{
	public interface ICollision
	{
		bool Enabled { get; set; }

		void Check(Projectile p);
	}
}
