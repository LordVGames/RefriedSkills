using HarmonyLib;
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


[MonoDetourTargets]
internal static class Desperado
{
    private const float _desperadoStackDuration = 12f;
    private const int _desperadoStacksOnBackstab = 2;
    private const int _desperadoStacksPerHemorrhage = 1;
    private const float _desperadoSkillRecharge = 6f;


    private static readonly AssetReferenceT<GameObject> _desperadoKillEffectReference = new(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Bandit2.Bandit2KillEffect_prefab);
    private static GameObject _desperadoKillEffect;
    private static readonly AssetReferenceT<SkillDef> _desperadoSkillReference = new(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Bandit2.SkullRevolver_asset);


    [MonoDetourHookInitialize]
    private static void Setup()
    {
        if (!ConfigOptions.Bandit.EnableNewDesperado.Value)
        {
            return;
        }
        ModLanguage.AddNewLangTokens("Desperado");
        AssetAsyncReferenceManager<GameObject>.LoadAsset(_desperadoKillEffectReference).Completed += (handle) =>
        {
            _desperadoKillEffect = handle.Result;
        };
        AssetAsyncReferenceManager<SkillDef>.LoadAsset(_desperadoSkillReference).Completed += (handle) =>
        {
            handle.Result.baseRechargeInterval = _desperadoSkillRecharge;
            handle.Result.keywordTokens = handle.Result.keywordTokens.AddToArray<string>("RFS_BANDIT2_DESPERADO_TOKENS_KEYWORD");
        };


        
        GlobalEventManager.onServerDamageDealt += GlobalEventManager_onServerDamageDealt;
        Mdh.RoR2.GlobalEventManager.OnCharacterDeath.ILHook(RemoveVanillaEffect);
    }


    private static void GlobalEventManager_onServerDamageDealt(DamageReport damageReport)
    {
        if (
            damageReport.attackerBody == null
            || damageReport.attackerBody.bodyIndex != RoR2Content.BodyPrefabs.Bandit2Body.bodyIndex
            || damageReport.attackerBody.skillLocator == null
            || damageReport.victimBody == null
            || (damageReport.damageInfo.damageType & DamageType.GiveSkullOnKill) != DamageType.GiveSkullOnKill
        )
        {
            return;
        }


        // ty ss2 for code to reference
        int desperadoBuffCount = 0;
        Vector3 attackerPosition = damageReport.attackerBody ? damageReport.attackerBody.corePosition : damageReport.damageInfo.attacker.transform.position;
        Vector3 between = damageReport.damageInfo.position - attackerPosition;
        if (BackstabManager.IsBackstab(between, damageReport.victimBody))
        {
            desperadoBuffCount += _desperadoStacksOnBackstab;
        }
        int hemorrhageBuffCount = damageReport.victimBody.GetBuffCount(RoR2Content.Buffs.SuperBleed);
        if (hemorrhageBuffCount > 0)
        {
            for (int i = 0; i < hemorrhageBuffCount; i++)
            {
                desperadoBuffCount += _desperadoStacksPerHemorrhage;
            }
        }


        if (desperadoBuffCount > 0)
        {
            for (int i = 0; i < desperadoBuffCount; i++)
            {
                damageReport.attackerBody.AddTimedBuff(RoR2Content.Buffs.BanditSkull, _desperadoStackDuration);
                damageReport.attackerBody.SetTimedBuffDurationIfPresent(RoR2Content.Buffs.BanditSkull, _desperadoStackDuration, true);
            }
            EffectManager.SpawnEffect(_desperadoKillEffect, new EffectData { origin = damageReport.damageInfo.position }, true);
        }
    }


    private static void RemoveVanillaEffect(ILManipulationInfo info)
    {
        ILWeaver w = new(info);
        ILLabel skipEffectLabel = null!;


        w.MatchRelaxed(
            x => x.MatchLdloc(1) && w.SetCurrentTo(x),
            x => x.MatchLdfld<DamageInfo>("damageType"),
            x => x.MatchLdcI4((int)DamageType.GiveSkullOnKill),
            x => x.MatchCallOrCallvirt(out _),
            x => x.MatchCallOrCallvirt(out _),
            x => x.MatchCallOrCallvirt(out _),
            x => x.MatchLdcI4((int)DamageType.GiveSkullOnKill),
            x => x.MatchBneUn(out skipEffectLabel)
        ).ThrowIfFailure()
        .InsertBeforeCurrentStealLabels(
            w.Create(OpCodes.Br, skipEffectLabel)
        );
    }
}