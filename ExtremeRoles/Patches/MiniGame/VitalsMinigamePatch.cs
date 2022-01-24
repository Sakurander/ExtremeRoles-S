﻿using HarmonyLib;

using UnityEngine;

namespace ExtremeRoles.Patches.MiniGame
{
    [HarmonyPatch(typeof(VitalsMinigame), nameof(VitalsMinigame.Update))]
    public class VitalsMinigameUpdatePatch
    {
        public static bool Prefix(VitalsMinigame __instance)
        {
            if (Roles.ExtremeRoleManager.GameRole[
                PlayerControl.LocalPlayer.PlayerId].CanUseVital) { return true; }

            __instance.SabText.text = "youDontUseThis";

            __instance.SabText.gameObject.SetActive(true);
			for (int j = 0; j < __instance.vitals.Length; j++)
			{
				__instance.vitals[j].gameObject.SetActive(false);
			}

            return false;
		}
    }
}
