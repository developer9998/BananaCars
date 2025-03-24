using BepInEx;
using HarmonyLib;
using Utilla.Attributes;

namespace BananaCars
{
    [ModdedGamemode]
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.5.0")]
    [BepInPlugin(Constants.Guid, Constants.Name, Constants.Version)]
    public class Plugin : BaseUnityPlugin
    {
        private Harmony harmony;

        private bool inModdedRoom;

        public void Awake()
        {
            Configuration.Init(Config);
        }


        [ModdedGamemodeJoin]
        public void OnRoomJoined(string gamemode)
        {
            inModdedRoom = true;
            Patch();
        }

        [ModdedGamemodeLeave]
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
