using BepInEx;
using HarmonyLib;

namespace BananaCars
{
    [BepInPlugin(Constants.Guid, Constants.Name, Constants.Version), BepInDependency("dev.tillahook", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        private Harmony harmony;

        private bool inModdedRoom;

        public void Awake()
        {
            Configuration.Init(Config);
            TillaHook.TillaHook.OnModdedJoin += OnRoomJoined;
            TillaHook.TillaHook.OnModdedLeave += OnRoomLeft;
            //TillaHook.TillaHook.Hook.AddGameMode(gameMode: null, OnRoomJoined, OnRoomLeft);
        }


        // [ModdedGamemodeJoin]
        public void OnRoomJoined(string gamemode)
        {
            inModdedRoom = true;
            Patch();
        }

        // [ModdedGamemodeLeave]
        public void OnRoomLeft(string gamemode)
        {
            inModdedRoom = false;
            Unpatch();
        }

        public void OnEnable()
        {
            Patch();
        }

        public void OnDisable()
        {
            Unpatch();
        }

        public void Patch()
        {
            if (enabled && inModdedRoom && harmony == null)
            {
                harmony = Harmony.CreateAndPatchAll(typeof(Plugin).Assembly, Constants.Guid);
            }
        }

        public void Unpatch()
        {
            if (harmony != null)
            {
                harmony.UnpatchSelf();
                harmony = null;
            }
        }
    }
}
