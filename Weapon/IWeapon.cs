using System;
using System.Collections.Generic;
using System.Text;

namespace WeaponsOverhaul
{
	public interface IWeapon
	{
		void Init(WeaponControlLayer layer);
		void Start();
		void SystemRestart();
		void Update();
		void Update100();
		void Animate();
		void Close();
	}
}
