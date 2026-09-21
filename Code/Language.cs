using R2API;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Text;
namespace RefriedSkills;


internal static class ModLanguage
{
    private static readonly string _rootlangFolderLocation = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Plugin.PluginInfo.Location), "Language");


    internal static void AddNewLangTokens(string languageFileName)
    {
        string itemLangFileLocation = System.IO.Path.Combine(_rootlangFolderLocation, $"{languageFileName}.json");
        LanguageAPI.AddOverlayPath(itemLangFileLocation);
    }
}