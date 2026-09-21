using EntityStates;
using EntityStates.Bandit2.Weapon;
using MonoDetour;
using MonoDetour.DetourTypes;
using MonoDetour.HookGen;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using RoR2BepInExPack.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
namespace RefriedSkills.Bandit;


[MonoDetourTargets(typeof(GenericBulletBaseState))]
[MonoDetourTargets(typeof(Bandit2FirePrimaryBase), GenerateControlFlowVariants = true)]
internal static class RiflePrimary
{
    private static FixedConditionalWeakTable<Bandit2FirePrimaryBase, ShotsFiredInfo> _banditsFiringRiflesTable = [];
    private class ShotsFiredInfo
    {
        internal int shotsFired;
    }
    private const int _shotsToFire = 2;
    private const float _timeBetweenShots = 0.1f;
    private const float _damageCoefficient = 1.5f;
    private const float _procCoefficient = 1f;




    [MonoDetourHookInitialize]
    private static void Setup()
    {
        if (!ConfigOptions.Bandit.EnableNewRiflePrimary.Value)
        {
            return;
        }


        ModLanguage.AddNewLangTokens("RiflePrimary");
        Mdh.EntityStates.GenericBulletBaseState.FixedUpdate.Prefix(BeforeFixedUpdate);
        Mdh.EntityStates.Bandit2.Weapon.Bandit2FirePrimaryBase.GetMinimumInterruptPriority.ControlFlowPrefix(Change);
        Mdh.EntityStates.Bandit2.Weapon.Bandit2FirePrimaryBase.OnEnter.Prefix(OnEnter);
    }


    private static void OnEnter(Bandit2FirePrimaryBase self)
    {
        if (self is not Bandit2FireRifle)
        {
            return;
        }


        self.damageCoefficient = _damageCoefficient;
        self.procCoefficient = _procCoefficient;
        self.duration = self.baseDuration / self.attackSpeedStat;
        // doing this to reset the shots fired each time we enter a new burst
        if (_banditsFiringRiflesTable.ContainsKey(self))
        {
            _banditsFiringRiflesTable.Remove(self);
        }
        _banditsFiringRiflesTable.TryAdd(self, new ShotsFiredInfo { shotsFired = 0 });
    }


    private static ReturnFlow Change(Bandit2FirePrimaryBase self, ref InterruptPriority returnValue)
    {
        if (self is not Bandit2FireRifle)
        {
            return ReturnFlow.None;
        }


        returnValue = InterruptPriority.Death;
        return ReturnFlow.SkipOriginal;
    }


    private static void BeforeFixedUpdate(GenericBulletBaseState self)
    {
        if (self is not Bandit2FireRifle)
        {
            return;
        }
        ShotsFiredInfo shotsFiredInfo;
        if (!_banditsFiringRiflesTable.TryGetValue(self as Bandit2FirePrimaryBase, out shotsFiredInfo))
        {
            Log.Error("Couldn't get entity state in table of rifle fires?");
            return;
        }
        float attackSpeedTimeBetweenShots = _timeBetweenShots / self.attackSpeedStat;
        if (shotsFiredInfo.shotsFired == _shotsToFire && self.fixedAge > attackSpeedTimeBetweenShots * (_shotsToFire + 1))
        {
            shotsFiredInfo.shotsFired = 0;
            self.outer.SetNextStateToMain();
            return;
        }


        if (self.fixedAge > attackSpeedTimeBetweenShots * (shotsFiredInfo.shotsFired + 1))
        {
            self.FireBullet(self.GetAimRay());
            self.PlayAnimation("Gesture, Additive", Bandit2FirePrimaryBase.FireMainWeaponStateHash, Bandit2FirePrimaryBase.FireMainWeaponParamHash, self.duration);
            // rifle loses accuracy like this at high attack speeds, so don't do more bloom when it's enough
            if (shotsFiredInfo.shotsFired == 1 && self.attackSpeedStat < 2)
            {
                self.characterBody.AddSpreadBloom(self.spreadBloomValue * 0.5f);
            }
            shotsFiredInfo.shotsFired++;
        }
    }
}