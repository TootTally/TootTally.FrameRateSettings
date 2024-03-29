﻿using BaboonAPI.Hooks.Initializer;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using TootTallyCore.Utils.TootTallyModules;
using TootTallySettings;
using UnityEngine;

namespace TootTallyFrameRateSettings
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("TootTallyCore", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("TootTallySettings", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin, ITootTallyModule
    {
        public static Plugin Instance;

        private const string CONFIG_NAME = "FrameRateSettings.cfg";
        private const string CONFIG_FIELD = "FrameRate";
        private const bool DEFAULT_CAPSETTING = true;
        private const bool DEFAULT_UNLISETTING = false;
        private const float DEFAULT_FPSSETTING = 144;
        public Options option;
        public ConfigEntry<bool> ModuleConfigEnabled { get; set; }
        public bool IsConfigInitialized { get; set; }
        public string Name { get => PluginInfo.PLUGIN_NAME; set => Name = value; }
        public static TootTallySettingPage settingPage;

        public static void LogInfo(string msg) => Instance.Logger.LogInfo(msg);
        public static void LogError(string msg) => Instance.Logger.LogError(msg);

        private void Awake()
        {
            if (Instance != null) return;
            Instance = this;

            GameInitializationEvent.Register(Info, TryInitialize);
        }

        private void TryInitialize()
        {
            ModuleConfigEnabled = TootTallyCore.Plugin.Instance.Config.Bind("Modules", "Frame Rate Settings", true, "Enable Frame Rate Settings Module");
            TootTallyModuleManager.AddModule(this);
        }

        public void LoadModule()
        {
            string configPath = Path.Combine(Paths.BepInExRootPath, "config/");
            ConfigFile config = new ConfigFile(configPath + CONFIG_NAME, true);
            option = new Options()
            {
                Cap_FPS_In_Menus = config.Bind(CONFIG_FIELD, "Cap FPS In Menus", DEFAULT_CAPSETTING),
                Max_FPS_In_Menus = config.Bind(CONFIG_FIELD, "Maximum FPS In Menus", DEFAULT_FPSSETTING),
                Max_FPS = config.Bind(CONFIG_FIELD, "Maximum FPS", DEFAULT_FPSSETTING),
                Unlimited = config.Bind(CONFIG_FIELD, "Unlimited", DEFAULT_UNLISETTING),
            };

            settingPage = TootTallySettingsManager.AddNewPage("Frame Rates", "Frame Rate Settings", 40, new Color(.1f, .1f, .1f, .1f));
            if (settingPage != null)
            {
                settingPage.AddToggle("CapFPSMenuToggle", option.Cap_FPS_In_Menus, (value) => ResolveSlider(settingPage, "CapFPSMenu", "Max FPS In Menu", value, option.Max_FPS_In_Menus));
                ResolveSlider(settingPage, "CapFPSMenu", "Max FPS In Menu", option.Cap_FPS_In_Menus.Value, option.Max_FPS_In_Menus);
                settingPage.AddToggle("UnlimitedFPSToggle", option.Unlimited, (value) => ResolveSlider(settingPage, "MaxFPS", "Max FPS In Game", !value, option.Max_FPS));
                ResolveSlider(settingPage, "MaxFPS", "Max FPS In Game", !option.Unlimited.Value, option.Max_FPS);
            }

            TootTallySettings.Plugin.TryAddThunderstoreIconToPageButton(Instance.Info.Location, Name, settingPage);

            Harmony.CreateAndPatchAll(typeof(FrameRateSettingsPatch), PluginInfo.PLUGIN_GUID);
            LogInfo($"Module loaded!");
        }

        public void ResolveSlider(TootTallySettingPage page, string sliderName, string text, bool value, ConfigEntry<float> configEntry)
        {
            if (value)
                page.AddSlider($"{sliderName}Slider", 30, 1000, 350, text, configEntry, true);
            else
                page.RemoveSettingObjectFromList($"{sliderName}Slider");
        }

        public void UnloadModule()
        {
            Harmony.UnpatchID(PluginInfo.PLUGIN_GUID);
            settingPage.Remove();
            LogInfo($"Module unloaded!");
        }

        public static class FrameRateSettingsPatch
        {
            #region Menu FPS Cap Patches
            /* Menu frame rate capping patches */
            [HarmonyPatch(typeof(SaveSlotController), nameof(SaveSlotController.Start))]
            [HarmonyPostfix]
            public static void PatchFPSOnSaveSlotController() => ResolveFPSCap();

            [HarmonyPatch(typeof(HomeController), nameof(HomeController.Start))]
            [HarmonyPostfix]
            public static void PatchFPSOnHomeController() => ResolveFPSCap();

            [HarmonyPatch(typeof(HomeController), nameof(HomeController.tryToSaveSettings))]
            [HarmonyPostfix]
            public static void PatchFPSOnSettingsSave() => ResolveFPSCap();

            [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.Start))]
            [HarmonyPostfix]
            public static void PatchFPSOnLevelSelectController() => ResolveFPSCap();

            [HarmonyPatch(typeof(PointSceneController), nameof(PointSceneController.Start))]
            [HarmonyPostfix]
            public static void PatchFPSOnPointSceneController() => ResolveFPSCap();

            [HarmonyPatch(typeof(GameController), nameof(GameController.Start))]
            [HarmonyPostfix]
            public static void PatchFPSOnGameController()
            {
                int fpsCap = (int)Plugin.Instance.option.Max_FPS.Value;
                bool unlimited = Plugin.Instance.option.Unlimited.Value;
                Application.targetFrameRate = unlimited ? -1 : fpsCap;
            }
            #endregion

            private static void ResolveFPSCap()
            {
                int fpsCap = (int)(Plugin.Instance.option.Unlimited.Value ? -1 : Plugin.Instance.option.Max_FPS.Value);
                if (Plugin.Instance.option.Cap_FPS_In_Menus.Value)
                    Application.targetFrameRate = (int)Plugin.Instance.option.Max_FPS_In_Menus.Value;
                else
                    Application.targetFrameRate = fpsCap;
            }
        }

        public class Options
        {
            public ConfigEntry<bool> Cap_FPS_In_Menus { get; set; }
            public ConfigEntry<bool> Unlimited { get; set; }
            public ConfigEntry<float> Max_FPS { get; set; }
            public ConfigEntry<float> Max_FPS_In_Menus { get; set; }

        }
    }
}