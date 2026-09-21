using MonoDetour;
using MonoDetour.HookGen;
using RoR2;
using RoR2.ContentManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
namespace RefriedSkills.Bandit;


[MonoDetourTargets]
internal static class DefaultSecondary
{
    private static readonly AssetReferenceT<GameObject> _banditBodyReference = new(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Bandit2.Bandit2Body_prefab);


    [MonoDetourHookInitialize]
    private static void Setup()
    {
        if (ModSupport.RiskyTweaksMod.ModIsRunning || !ConfigOptions.Bandit.EnableNewSecondaries.Value)
        {
            return;
        }


        AssetAsyncReferenceManager<GameObject>.LoadAsset(_banditBodyReference).Completed += (handle) =>
        {
            // shoutouts to RIskyTweaks for this stuff
            CharacterBody characterBody = handle.Result.GetComponent<CharacterBody>();
            HitBoxGroup hitboxGroup = characterBody.GetComponentInChildren<HitBoxGroup>();
            if (hitboxGroup.groupName == "SlashBlade")
            {
                //10.35, 4.25, 5.73
                Transform hitboxTransform = hitboxGroup.hitBoxes[0].transform;
                hitboxTransform.localScale = new Vector3(10.35f, 6f, 7.5f);
                hitboxTransform.localPosition += new Vector3(0f, 0f, 1f);
            }
        };
    }
}