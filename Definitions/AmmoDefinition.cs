using ProtoBuf;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRageMath;
using WeaponsOverhaul.Definitions;

namespace WeaponsOverhaul
{
	[ProtoContract]
	public class AmmoDefinition
	{
		[ProtoMember(1)]
		public bool Enabled;

		// From MyAmmoDefinition
		[ProtoMember(10)]
		public string SubtypeId;

		[ProtoMember(20)]
		public float DesiredSpeed;

		[ProtoMember(21)]
		public float SpeedVariance;

		[ProtoMember(30)]
		public float MaxTrajectory;

		[ProtoMember(40)]
		public float BackkickForce;

		//[ProtoMember(41)]
		//public string PhysicalMaterial;


		// From MyProjectileAmmoDefinition
		[ProtoMember(50)]
		public float ProjectileHitImpulse;

		[ProtoMember(60)]
		public float ProjectileTrailScale;

		[ProtoMember(70)]
		public Vector3 ProjectileTrailColor;

		[ProtoMember(71)]
		public string ProjectileTrailMaterial;

		[ProtoMember(80)]
		public float ProjectileTrailProbability;

		[ProtoMember(81)]
		public string ProjectileOnHitEffectName;

		[ProtoMember(90)]
		public float ProjectileMassDamage;

		[ProtoMember(100)]
		public float ProjectileHealthDamage;

		[ProtoMember(110)]
		public float ProjectileHeadShotDamage;

		[ProtoMember(120)]
		public int ProjectileCount;

		[ProtoMember(130)]
		public RicochetDefinition Ricochet;

		public void Copy(AmmoDefinition a)
		{
			SubtypeId = a.SubtypeId;
			DesiredSpeed = a.DesiredSpeed;
			SpeedVariance = a.SpeedVariance;
			MaxTrajectory = a.MaxTrajectory;
			BackkickForce = a.BackkickForce;
			//PhysicalMaterial = a.PhysicalMaterial;

			ProjectileHitImpulse = a.ProjectileHitImpulse;
			ProjectileTrailScale = a.ProjectileTrailScale;
			ProjectileTrailColor = a.ProjectileTrailColor;
			ProjectileTrailMaterial = a.ProjectileTrailMaterial;
			ProjectileTrailProbability = a.ProjectileTrailProbability;
			ProjectileOnHitEffectName = a.ProjectileOnHitEffectName;
			ProjectileMassDamage = a.ProjectileMassDamage;
			ProjectileHealthDamage = a.ProjectileHealthDamage;
			ProjectileHeadShotDamage = a.ProjectileHeadShotDamage;
			ProjectileCount = a.ProjectileCount;

			Ricochet = Ricochet.Clone();
		}

		public AmmoDefinition Clone()
		{
			return new AmmoDefinition() {
				Enabled = Enabled,
				SubtypeId = SubtypeId,
				DesiredSpeed = DesiredSpeed,
				SpeedVariance = SpeedVariance,
				MaxTrajectory = MaxTrajectory,
				BackkickForce = BackkickForce,
				//PhysicalMaterial = PhysicalMaterial,

				ProjectileHitImpulse = ProjectileHitImpulse,
				ProjectileTrailScale = ProjectileTrailScale,
				ProjectileTrailColor = ProjectileTrailColor,
				ProjectileTrailMaterial = ProjectileTrailMaterial,
				ProjectileTrailProbability = ProjectileTrailProbability,
				ProjectileOnHitEffectName = ProjectileOnHitEffectName,
				ProjectileMassDamage = ProjectileMassDamage,
				ProjectileHealthDamage = ProjectileHealthDamage,
				ProjectileHeadShotDamage = ProjectileHeadShotDamage,
				ProjectileCount = ProjectileCount,
				Ricochet = Ricochet.Clone(),
			};
		}

		public static AmmoDefinition CreateFromKeenDefinition(MyProjectileAmmoDefinition p)
		{
			return new AmmoDefinition {
				Enabled = true,
				SubtypeId = p.Id.SubtypeId.String,
				DesiredSpeed = p.DesiredSpeed,
				SpeedVariance = p.SpeedVar,
				MaxTrajectory = p.MaxTrajectory,
				BackkickForce = p.BackkickForce,
				//PhysicalMaterial = p.PhysicalMaterial.ToString(),

				ProjectileHitImpulse = p.ProjectileHitImpulse,
				ProjectileTrailScale = p.ProjectileTrailScale,
				ProjectileTrailColor = p.ProjectileTrailColor,
				ProjectileTrailMaterial = p.ProjectileTrailMaterial,
				ProjectileTrailProbability = p.ProjectileTrailProbability,
				ProjectileOnHitEffectName = p.ProjectileOnHitEffectName,
				ProjectileMassDamage = p.ProjectileMassDamage,
				ProjectileHealthDamage = p.ProjectileHealthDamage,
				ProjectileHeadShotDamage = p.ProjectileHeadShotDamage,
				ProjectileCount = p.ProjectileCount,

				Ricochet = new RicochetDefinition() {
					DeflectionAngle = 45,
					MaxVelocityTransfer = 0.5f,
					MaxDamageTransfer = 0.5f,
					RicochetChance = 1,
				},
			};
		}

		public override string ToString()
		{
			return MyAPIGateway.Utilities.SerializeToXML(this);
		}
	}
}
