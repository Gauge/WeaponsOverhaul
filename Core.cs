using SENetworkAPI;
using VRage.Game;
using VRage.Game.Components;

namespace WeaponsOverhaul
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation | MyUpdateOrder.BeforeSimulation)]
    public class Core : MyNetworkSessionComponent
    {
        public const ushort ModId = 12144;
        public const string ModName = "WeaponsOverhaul";
        public const string ModKeyword = "wo";

        //private NetSync<Settings>


        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            if (!NetworkAPI.IsInitialized)
            {
                NetworkAPI.Init(ModId, ModName, ModKeyword);
            }

            Settings.Load();
        }
    }
}

