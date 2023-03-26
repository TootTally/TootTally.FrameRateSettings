using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.IO;
using TootTally.Utils;
using UnityEngine;

namespace TootTally.FrameRateSettings
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin, ITootTallyModule
    {
        public static Plugin Instance;

        private const string CONFIG_NAME = "FrameRateSettings.cfg";
        private const string CONFIG_FIELD = "FrameRate";
        private const bool DEFAULT_CAPSETTING = true;
        private const bool DEFAULT_UNLISETTING = false;
        private const int DEFAULT_FPSSETTING = 144;
        public Options option;
        public ConfigEntry<bool> ModuleConfigEnabled { get; set; }
        public bool IsConfigInitialized { get; set; }
        public string Name { get => PluginInfo.PLUGIN_NAME; set => Name = value; }
        public void LogInfo(string msg) => Logger.LogInfo(msg);
        public void LogError(string msg) => Logger.LogError(msg);

        private void Awake()
        {
            if (Instance != null) return;
            Instance = this;

            ModuleConfigEnabled = TootTally.Plugin.Instance.Config.Bind("Modules", "Frame Rate Settings", true, "Enable Frame Rate Settings Module");
            OptionalTrombSettings.Add(TootTally.Plugin.Instance.moduleSettings, ModuleConfigEnabled);
            TootTally.Plugin.AddModule(this);
        }

        public void LoadModule()
        {
            string configPath = Path.Combine(Paths.BepInExRootPath, "config/");
            ConfigFile config = new ConfigFile(configPath + CONFIG_NAME, true);
            option = new Options()
            {
                Cap_FPS_In_Menus = config.Bind(CONFIG_FIELD, nameof(option.Cap_FPS_In_Menus), DEFAULT_CAPSETTING),
                Max_FPS = config.Bind(CONFIG_FIELD, nameof(option.Max_FPS), DEFAULT_FPSSETTING),
                Unlimited = config.Bind(CONFIG_FIELD, nameof(option.Unlimited), DEFAULT_UNLISETTING),
            };

            var settingsPage = OptionalTrombSettings.GetConfigPage("Frame Rates");
            if (settingsPage != null) {
                OptionalTrombSettings.Add(settingsPage, option.Cap_FPS_In_Menus);
                OptionalTrombSettings.Add(settingsPage, option.Unlimited);
                OptionalTrombSettings.AddSlider(settingsPage, 30, 1000, 1.0f, true, option.Max_FPS);
            }

            Harmony.CreateAndPatchAll(typeof(FrameRateSettingsPatch), PluginInfo.PLUGIN_GUID);
            LogInfo($"Module loaded!");
        }

        public void UnloadModule()
        {
            Harmony.UnpatchID(PluginInfo.PLUGIN_GUID);
            LogInfo($"Module unloaded!");
        }

        public static class FrameRateSettingsPatch
        {
            /* Menu frame rate capping patches */
            [HarmonyPatch(typeof(SaveSlotController), nameof(SaveSlotController.Start))]
            [HarmonyPostfix]
            public static void PatchFPSOnSaveSlotController()
            {
                int fpsCap = Plugin.Instance.option.Unlimited.Value ? -1 : Plugin.Instance.option.Max_FPS.Value;
                if (Plugin.Instance.option.Cap_FPS_In_Menus.Value)
                    Application.targetFrameRate = (fpsCap <= 144 && fpsCap > 0) ? fpsCap : 144;
                else
                    Application.targetFrameRate = fpsCap;
            }

            [HarmonyPatch(typeof(HomeController), nameof(HomeController.Start))]
            [HarmonyPostfix]
            public static void PatchFPSOnHomeController()
            {
                int fpsCap = Plugin.Instance.option.Unlimited.Value ? -1 : Plugin.Instance.option.Max_FPS.Value;
                if (Plugin.Instance.option.Cap_FPS_In_Menus.Value)
                    Application.targetFrameRate = (fpsCap <= 144 && fpsCap > 0) ? fpsCap : 144;
                else
                    Application.targetFrameRate = fpsCap;
            }

            [HarmonyPatch(typeof(HomeController), nameof(HomeController.tryToSaveSettings))]
            [HarmonyPostfix]
            public static void PatchFPSOnSettingsSave()
            {
                int fpsCap = Plugin.Instance.option.Unlimited.Value ? -1 : Plugin.Instance.option.Max_FPS.Value;
                if (Plugin.Instance.option.Cap_FPS_In_Menus.Value)
                    Application.targetFrameRate = (fpsCap <= 144 && fpsCap > 0) ? fpsCap : 144;
                else
                    Application.targetFrameRate = fpsCap;
            }

            [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.Start))]
            [HarmonyPostfix]
            public static void PatchFPSOnLevelSelectController()
            {
                int fpsCap = Plugin.Instance.option.Unlimited.Value ? -1 : Plugin.Instance.option.Max_FPS.Value;
                if (Plugin.Instance.option.Cap_FPS_In_Menus.Value)
                    Application.targetFrameRate = (fpsCap <= 144 && fpsCap > 0) ? fpsCap : 144;
                else
                    Application.targetFrameRate = fpsCap;
            }

            [HarmonyPatch(typeof(PointSceneController), nameof(PointSceneController.Start))]
            [HarmonyPostfix]
            public static void PatchFPSOnPointSceneController()
            {
                int fpsCap = Plugin.Instance.option.Unlimited.Value ? -1 : Plugin.Instance.option.Max_FPS.Value;
                if (Plugin.Instance.option.Cap_FPS_In_Menus.Value)
                    Application.targetFrameRate = (fpsCap <= 144 && fpsCap > 0) ? fpsCap : 144;
                else
                    Application.targetFrameRate = fpsCap;
            }

            [HarmonyPatch(typeof(GameController), nameof(GameController.Start))]
            [HarmonyPostfix]
            public static void PatchFPSOnGameController()
            {
                int fpsCap = Plugin.Instance.option.Max_FPS.Value;
                bool unlimited = Plugin.Instance.option.Unlimited.Value;
                Application.targetFrameRate = unlimited ? -1 : fpsCap;
            }
        }

        public class Options
        {
            public ConfigEntry<bool> Cap_FPS_In_Menus { get; set; }
            public ConfigEntry<bool> Unlimited { get; set; }
            public ConfigEntry<int> Max_FPS { get; set; }

        }
    }
}