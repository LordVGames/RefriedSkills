using EntityStates;
using EntityStates.Bandit2.Weapon;
using Mono.Cecil.Cil;
using MonoDetour;
using MonoDetour.Cil;
using MonoDetour.DetourTypes;
using MonoDetour.HookGen;
using MonoMod.Cil;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
namespace RefriedSkills.Bandit.LightsOut;


[MonoDetourTargets(typeof(GlobalEventManager))]
internal static class LightsOutSkill
{
    private const float _lightsOutSkillRecharge = 6f;


    private static readonly AssetReferenceT<GameObject> _lightsOutKillEffectReference = new(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Bandit2.Bandit2ResetEffect_prefab);
    private static GameObject _lightsOutKillEffect;
    private static readonly AssetReferenceT<SkillDef> _lightsOutSkillReference = new(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Bandit2.ResetRevolver_asset);


    [MonoDetourHookInitialize]
    private static void Setup()
    {
        if (!ConfigOptions.Bandit.EnableNewLightsOut.Value)
        {
            return;
        }
        ModLanguage.AddNewLangTokens("LightsOut");
        AssetAsyncReferenceManager<GameObject>.LoadAsset(_lightsOutKillEffectReference).Completed += (handle) =>
        {
            _lightsOutKillEffect = handle.Result;
        };
        AssetAsyncReferenceManager<SkillDef>.LoadAsset(_lightsOutSkillReference).Completed += (handle) =>
        {
            handle.Result.baseRechargeInterval = _lightsOutSkillRecharge;
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
            || (damageReport.damageInfo.damageType & DamageType.ResetCooldownsOnKill) != DamageType.ResetCooldownsOnKill
        )
        {
            return;
        }


        // ty ss2 for code to reference
        Vector3 attackerPosition = damageReport.attackerBody ? damageReport.attackerBody.corePosition : damageReport.damageInfo.attacker.transform.position;
        Vector3 between = damageReport.damageInfo.position - attackerPosition;
        bool doEffect = false;
        bool syncPrimaryAndSecondaryRecharge = false;
        bool syncSpecialRecharge = false;
        if (damageReport.victimBody.GetBuffCount(RoR2Content.Buffs.SuperBleed) > 0)
        {
            doEffect = true;
            syncPrimaryAndSecondaryRecharge = true;
            damageReport.attackerBody.skillLocator.primary.ResetStock();
            damageReport.attackerBody.skillLocator.secondary.RechargeBaseSkill(99);
            
        }
        if (BackstabManager.IsBackstab(between, damageReport.victimBody))
        {
            doEffect = true;
            syncSpecialRecharge = true;
            damageReport.attackerBody.skillLocator.special.RechargeBaseSkill(99);
        }
        if (doEffect)
        {
            EffectManager.SpawnEffect(_lightsOutKillEffect, new EffectData { origin = damageReport.damageInfo.position }, true);
        }
        if (!damageReport.attackerBody.gameObject.TryGetComponent<NetworkIdentity>(out var netIdentity))
        {
            Log.Error("Couldn't get net ID from a bandit body?");
            return;
        }
        if (syncSpecialRecharge || syncPrimaryAndSecondaryRecharge)
        {
            new SyncRechargesFromNewLightsOut(netIdentity.netId, syncPrimaryAndSecondaryRecharge, syncSpecialRecharge).Send(R2API.Networking.NetworkDestination.Clients);
        }
    }


    private static void RemoveVanillaEffect(ILManipulationInfo info)
    {
        ILWeaver w = new(info);
        ILLabel skipEffectLabel = null!;


        w.MatchRelaxed(
            x => x.MatchLdloc(1) && w.SetCurrentTo(x),
            x => x.MatchLdfld<DamageInfo>("damageType"),
            x => x.MatchLdcI4((int)DamageType.ResetCooldownsOnKill),
            x => x.MatchCallOrCallvirt(out _),
            x => x.MatchCallOrCallvirt(out _),
            x => x.MatchCallOrCallvirt(out _),
            x => x.MatchLdcI4((int)DamageType.ResetCooldownsOnKill),
            x => x.MatchBneUn(out skipEffectLabel)
        ).ThrowIfFailure()
        .InsertBeforeCurrentStealLabels(
            w.Create(OpCodes.Br, skipEffectLabel)
        );
    }
}