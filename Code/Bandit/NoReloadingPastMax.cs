using EntityStates.Bandit2.Weapon;
using MonoDetour;
using MonoDetour.DetourTypes;
using MonoDetour.HookGen;
using System;
using System.Collections.Generic;
using System.Text;
namespace RefriedSkills.Bandit;


[MonoDetourTargets(typeof(Reload), GenerateControlFlowVariants = true)]
internal static class NoReloadingPastMax
{
    [MonoDetourHookInitialize]
    private static void Setup()
    {
        Mdh.EntityStates.Bandit2.Weapon.Reload.FixedUpdate.ControlFlowPrefix(StopReloadingDude);
    }


    // fuckass bandaid fix for clients reloading past 4 when resetting all their stocks (but hosts dont?????)
    private static ReturnFlow StopReloadingDude(Reload self)
    {
        if (self.skillLocator.primary.stock > 3)
        {
            return ReturnFlow.SkipOriginal;
        }
        return ReturnFlow.None;
    }
}