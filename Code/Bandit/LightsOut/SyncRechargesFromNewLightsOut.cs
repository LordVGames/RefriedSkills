using R2API.Networking.Interfaces;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
namespace RefriedSkills.Bandit.LightsOut;


internal class SyncRechargesFromNewLightsOut : INetMessage
{
    NetworkInstanceId attackerBodyNetID;
    bool syncPrimaryAndSecondaryRecharge;
    bool syncSpecialRecharge;


    public SyncRechargesFromNewLightsOut()
    {
    }
    public SyncRechargesFromNewLightsOut(NetworkInstanceId netId, bool syncPrimaryAndSecondaryRecharge, bool syncSpecialRecharge)
    {
        this.attackerBodyNetID = netId;
        this.syncPrimaryAndSecondaryRecharge = syncPrimaryAndSecondaryRecharge;
        this.syncSpecialRecharge = syncSpecialRecharge;
    }


    public void Serialize(NetworkWriter writer)
    {
        writer.Write(attackerBodyNetID);
        writer.Write(syncPrimaryAndSecondaryRecharge);
        writer.Write(syncSpecialRecharge);
    }


    public void Deserialize(NetworkReader reader)
    {
        attackerBodyNetID = reader.ReadNetworkId();
        syncPrimaryAndSecondaryRecharge= reader.ReadBoolean();
        syncSpecialRecharge = reader.ReadBoolean();
    }


    public void OnReceived()
    {
        if (NetworkServer.active)
        {
            return;
        }
        GameObject attackerBodyObject = Util.FindNetworkObject(attackerBodyNetID);
        if (!attackerBodyObject.TryGetComponent<CharacterBody>(out var attackerBody))
        {
            Log.Error("SYNC MESSAGE Couldn't get attacker CharacterBody from their GameObject!");
            return;
        }


        if (syncPrimaryAndSecondaryRecharge)
        {
            attackerBody.skillLocator.primary.ResetStock();
            attackerBody.skillLocator.secondary.RechargeBaseSkill(99);
        }
        if (syncSpecialRecharge)
        {
            attackerBody.skillLocator.special.RechargeBaseSkill(99);
        }
    }
}