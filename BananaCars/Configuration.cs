using BepInEx.Configuration;

namespace BananaCars
{
    public static class Configuration
    {
        private static ConfigFile File;

        public static ConfigEntry<float> Accceleration, Limit, DeadZone;

        public static ConfigEntry<bool> Experiment;

        public static void Init(ConfigFile file)
        {
            File = file;
            Accceleration = File.Bind("Physics", "Acceleration", 5f, "The amount of speed added per second");
            Limit = File.Bind("Physics", "Limit", 7.5f, "The maximum amount of speed you can get to");
            DeadZone = File.Bind("Input", "Deadzone", 0.1f, "The minimum distance for your control stick for it to output");
            Experiment = File.Bind("Experimental", "Use Experimental Movement", false, "The experimental movement has more smooth movement and drifting");
        }
    }
}
