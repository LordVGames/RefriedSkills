using EntityStates.Bandit2.Weapon;
using Mono.Cecil.Cil;
using MonoDetour;
using MonoDetour.Cil;
using MonoDetour.HookGen;
using MonoMod.Cil;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
namespace RefriedSkills.Bandit;


[MonoDetourTargets(typeof(SlashBlade))]
internal static class BothSecondaries
{
    private static readonly AssetReferenceT<SkillDef> _meleeSecondarySkillReference = new(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Bandit2.SlashBlade_asset);
    private static SkillDef _meleeSecondarySkill;
    private static readonly AssetReferenceT<SkillDef> _rangedSecondarySkillReference = new(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Bandit2.Bandit2SerratedShivs_asset);
    private static SkillDef _rangedSecondarySkill;


    [MonoDetourHookInitialize]
    private static void Setup()
    {
        if (!ConfigOptions.Bandit.EnableNewSecondaries.Value)
        {
            return;
        }


        ModLanguage.AddNewLangTokens("BothSecondaries");
        AssetAsyncReferenceManager<SkillDef>.LoadAsset(_meleeSecondarySkillReference).Completed += (handle) =>
        {
            _meleeSecondarySkill = handle.Result;
        };
        AssetAsyncReferenceManager<SkillDef>.LoadAsset(_rangedSecondarySkillReference).Completed += (handle) =>
        {
            _rangedSecondarySkill = handle.Result;
        };
        Mdh.RoR2.GlobalEventManager.ProcessHitEnemy.ILHook(RemoveVanillaEffects);
        GlobalEventManager.onServerDamageDealt += GlobalEventManager_onServerDamageDealt;
    }


    private static void RemoveVanillaEffects(ILManipulationInfo info)
    {
        ILWeaver w = new(info);
        ILLabel skipEffectLabel = w.DefineLabel();


        w.MatchRelaxed(
            x => x.MatchLdarg(1) && w.SetCurrentTo(x),
            x => x.MatchLdfld<DamageInfo>("crit"),
            x => x.MatchBrfalse(out skipEffectLabel),
            x => x.MatchLdarg(1),
            x => x.MatchLdfld<DamageInfo>("damageType"),
            x => x.MatchLdcI4((int)DamageType.SuperBleedOnCrit)
        ).ThrowIfFailure()
        .InsertBeforeCurrentStealLabels(
            w.Create(OpCodes.Br, skipEffectLabel)
        );
    }


    private static void GlobalEventManager_onServerDamageDealt(DamageReport damageReport)
    {
        if (
            // surely theres a way where i dont have to do a bajillion checks here
            damageReport.attackerBody == null
            || damageReport.attackerBody.bodyIndex != RoR2Content.BodyPrefabs.Bandit2Body.bodyIndex
            || damageReport.victimBody == null
            || !damageReport.damageInfo.crit
            || damageReport.damageInfo.damageType.damageSource != DamageSource.Secondary
            || (damageReport.damageInfo.damageType & DamageType.SuperBleedOnCrit) != DamageType.SuperBleedOnCrit
            || damageReport.attackerBody.skillLocator == null
        )
        {
            return;
        }
        Vector3 attackerPosition = damageReport.attackerBody ? damageReport.attackerBody.corePosition : damageReport.damageInfo.attacker.transform.position;
        Vector3 between = damageReport.damageInfo.position - attackerPosition;
        if (!BackstabManager.IsBackstab(between, damageReport.victimBody))
        {
            return;
        }



        if (damageReport.attackerBody.skillLocator.secondary.skillNameToken == _meleeSecondarySkill.skillNameToken)
        {
            DotController.InflictDot(damageReport.victimBody.gameObject, damageReport.damageInfo.attacker, damageReport.damageInfo.inflictedHurtbox, DotController.DotIndex.SuperBleed, 15f * damageReport.damageInfo.procCoefficient, 1f);
            DotController.InflictDot(damageReport.victimBody.gameObject, damageReport.damageInfo.attacker, damageReport.damageInfo.inflictedHurtbox, DotController.DotIndex.SuperBleed, 15f * damageReport.damageInfo.procCoefficient, 1f);
        }
        else if (damageReport.attackerBody.skillLocator.secondary.skillNameToken == _rangedSecondarySkill.skillNameToken)
        {
            DotController.InflictDot(damageReport.victimBody.gameObject, damageReport.damageInfo.attacker, damageReport.damageInfo.inflictedHurtbox, DotController.DotIndex.SuperBleed, 15f * damageReport.damageInfo.procCoefficient, 1f);
        }
    }
}