using BepInEx;
using HarmonyLib;
using Utilla;

namespace BananaCars
{
    [BepInPlugin(Constants.Guid, Constants.Name, Constants.Version), ModdedGamemode]
    public class Plugin : BaseUnityPlugin
    {
        private Harmony harmony;

        private bool inModdedRoom;

        public void Awake()
        {
            Configuration.Init(Config);
        }

        [ModdedGamemodeJoin]
        public void OnRoomJoined()
        {
            inModdedRoom = true;
            Patch();
        }

        [ModdedGamemodeLeave]
        public void OnRoomLeft()
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
