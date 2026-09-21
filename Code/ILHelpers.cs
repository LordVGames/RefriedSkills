using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using MonoDetour.Cil;
namespace RefriedSkills;


internal static class ILHelpers
{
    internal static void LogILAroundCurrent(this ILWeaver w, int range)
    {
        for (int i = range * -1; i < (range + 1); i++)
        {
            if (i == 0)
            {
                Log.Warning("");
                Log.Warning($"{w.Current}    CURRENT"); // shows as IL_0000 for some reason?
                Log.Warning("");
            }
            else
            {
                Log.Warning(w.Instructions[w.Index + i]);
            }
        }
    }
}