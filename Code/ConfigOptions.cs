using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using MiscFixes.Modules;
namespace RefriedSkills;


public static class ConfigOptions
{
    internal static ConfigEntry<bool> ShowDebugLogging;


    public static class Bandit
    {
        private const string _sectionName = "Bandit";
        public static ConfigEntry<bool> EnableNewRiflePrimary;
        public static ConfigEntry<bool> EnableNewSecondaries;
        public static ConfigEntry<bool> EnableNewLightsOut;
        public static ConfigEntry<bool> EnableNewDesperado;


        internal static void BindConfigOptions(ConfigFile config)
        {
            EnableNewRiflePrimary = config.BindOption(
                _sectionName,
                "Enable change(s) for Blast",
                "",
                true,
                Extensions.ConfigFlags.RestartRequired
            );
            EnableNewSecondaries = config.BindOption(
                _sectionName,
                "Enable change(s) for Serrated Dagger and Serrated Shiv",
                "",
                true,
                Extensions.ConfigFlags.RestartRequired
            );
            EnableNewLightsOut = config.BindOption(
                _sectionName,
                "Enable change(s) for Lights Out",
                "",
                true,
                Extensions.ConfigFlags.RestartRequired
            );
            EnableNewDesperado = config.BindOption(
                _sectionName,
                "Enable change(s) for Desperado",
                "",
                true,
                Extensions.ConfigFlags.RestartRequired
            );
        }
    }


    internal static void BindAllConfigOptions(ConfigFile config)
    {
        ShowDebugLogging = config.BindOption(
            "General stuff",
            "Show debug logging",
            "does what it says",
            false
        );
        Bandit.BindConfigOptions(config);
    }
}
