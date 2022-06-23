﻿using HarmonyLib;
using ExtremeRoles.Roles;

namespace ExtremeRoles.Patches
{

    [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.FixedUpdate))]
    public class CustomNetworkTransformPatch
    {
        public static void Postfix(CustomNetworkTransform __instance)
        {
            if (!ExtremeRolesPlugin.GameDataStore.IsRoleSetUpEnd()) { return; }
            if (ExtremeRoleManager.GameRole.Count == 0) { return; }

            byte playerId = __instance.gameObject.GetComponent<PlayerControl>().PlayerId;
            var role = ExtremeRoleManager.GameRole[playerId];

            if (role.IsBoost &&
                !__instance.AmOwner && 
                __instance.interpolateMovement != 0.0f)
            {
                __instance.body.velocity *= role.MoveSpeed;
            }
        }
    }
}
