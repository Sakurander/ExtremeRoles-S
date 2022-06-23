﻿using HarmonyLib;

using ExtremeRoles.GhostRoles;
using ExtremeRoles.Roles;
using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.API.Interface;
using ExtremeRoles.Performance;


namespace ExtremeRoles.Patches.Manager
{

    // コレを消すと動かなくなる
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
    public static class HudManagerStartPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
        public static void PostFix(HudManager __instance)
        {
            if (Helper.GameSystem.IsFreePlay) { return; }

            if (Module.InfoOverlay.Button.Body == null)
            {
                Module.InfoOverlay.Button.CreateInfoButton();
            }
            else
            {
                Module.InfoOverlay.Button.SetInfoButtonToGameStartShipPositon();
            }
        }
    }


    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public class HudManagerUpdatePatch
    {
        private static bool buttonCreated = false;
        public static void Prefix(HudManager __instance)
        {
            if (__instance.GameSettings != null)
            {
                __instance.GameSettings.fontSize = 1.2f;
            }
            if (ExtremeRolesPlugin.GameDataStore.AssassinMeetingTrigger)
            {

                __instance.UseButton.ToggleVisible(false);
                __instance.AbilityButton.ToggleVisible(false);
                __instance.ReportButton.ToggleVisible(false);
                __instance.KillButton.ToggleVisible(false);
                __instance.SabotageButton.ToggleVisible(false);
                __instance.ImpostorVentButton.ToggleVisible(false);
                __instance.TaskText.transform.parent.gameObject.SetActive(false);
                __instance.roomTracker.gameObject.SetActive(false);
                
                IVirtualJoystick virtualJoystick = __instance.joystick;

                if (virtualJoystick != null)
                {
                    virtualJoystick.ToggleVisuals(false);
                }
            }

        }
        public static void Postfix(HudManager __instance)
        {
            if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started) { return; }
            if (!ExtremeRolesPlugin.GameDataStore.IsRoleSetUpEnd()) { return; }
            if (ExtremeRoleManager.GameRole.Count == 0) { return; }

            if (AmongUsClient.Instance.AmHost)
            {
                for (int i = 0; i < ExtremeRolesPlugin.GameDataStore.UpdateObject.Count; ++i)
                {
                    ExtremeRolesPlugin.GameDataStore.UpdateObject[i].Update(i);
                }
            }
            
            var role = ExtremeRoleManager.GetLocalPlayerRole();

            buttonCreate(role);
            roleUpdate(role);

            
            var multiAssignRole = role as MultiAssignRoleBase;
            if (multiAssignRole != null)
            {
                if (multiAssignRole.AnotherRole != null)
                {
                    buttonCreate(multiAssignRole.AnotherRole);
                    roleUpdate(multiAssignRole.AnotherRole);
                    multiAssignRole.OverrideAnotherRoleSetting();
                }
            }

            var ghostRole = ExtremeGhostRoleManager.GetLocalPlayerGhostRole();
            if (ghostRole != null)
            {
                role.HasTask = role.HasTask && ghostRole.HasTask;
            }

        }
        private static void buttonCreate(SingleRoleBase checkRole)
        {
            if (buttonCreated) { return; }

            var abilityRole = checkRole as IRoleAbility;

            if (abilityRole != null)
            {
                if (abilityRole.Button == null)
                {
                    buttonCreated = true; //一時的にブロック
                    
                    abilityRole.CreateAbility();
                    abilityRole.RoleAbilityInit();

                    buttonCreated = abilityRole.Button != null; // 作れたかどうか
                }
                else
                {
                    buttonCreated = true;
                }
            }
        }

        private static void roleUpdate(SingleRoleBase checkRole)
        {
            var updatableRole = checkRole as IRoleUpdate;
            if (updatableRole != null)
            {
                updatableRole.Update(CachedPlayerControl.LocalPlayer);
            }
        }

    }
}
