﻿using System;
using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;

using UnhollowerBaseLib;

using ExtremeRoles.GhostRoles;
using ExtremeRoles.Roles;
using ExtremeRoles.Roles.API.Interface;
using ExtremeRoles.Performance;

namespace ExtremeRoles.Patches.Meeting
{

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.BloopAVoteIcon))]
    class MeetingHudBloopAVoteIconPatch
    {
        public static bool Prefix(
            MeetingHud __instance,
            [HarmonyArgument(0)] GameData.PlayerInfo voterPlayer,
            [HarmonyArgument(1)] int index, [HarmonyArgument(2)] Transform parent)
        {

            if (ExtremeRoleManager.GameRole.Count == 0) { return true; }

            SpriteRenderer spriteRenderer = UnityEngine.Object.Instantiate<SpriteRenderer>(
                __instance.PlayerVotePrefab);

            var role = ExtremeRoleManager.GetLocalPlayerRole();
            
            bool canSeeVote = false;
            
            var mariln = role as Roles.Combination.Marlin;
            var assassin = role as Roles.Combination.Assassin;

            if (mariln != null)
            {
                canSeeVote = mariln.CanSeeVote;
            }
            if (assassin != null)
            {
                canSeeVote = assassin.CanSeeVote;
            }


            if (!PlayerControl.GameOptions.AnonymousVotes || canSeeVote ||
                (CachedPlayerControl.LocalPlayer.Data.IsDead && OptionHolder.Client.GhostsSeeVote &&
                 !isVoteSeeBlock(role)))
            {
                PlayerMaterial.SetColors(voterPlayer.DefaultOutfit.ColorId, spriteRenderer);
            }
            else
            {
                PlayerMaterial.SetColors(Palette.DisabledGrey, spriteRenderer);
            }

            spriteRenderer.transform.SetParent(parent);
            spriteRenderer.transform.localScale = Vector3.zero;

            __instance.StartCoroutine(
                Effects.Bloop((float)index * 0.3f,
                spriteRenderer.transform, 1f, 0.5f));
            parent.GetComponent<VoteSpreader>().AddVote(spriteRenderer);

            return false;
        }

        private static bool isVoteSeeBlock(Roles.API.SingleRoleBase role)
        {
            if (ExtremeGhostRoleManager.GameRole.ContainsKey(
                    CachedPlayerControl.LocalPlayer.PlayerId) ||
                CachedPlayerControl.LocalPlayer.Data.Role.Role == RoleTypes.GuardianAngel)
            {
                return true;
            }
            else if (role.IsImpostor() && role.Id != ExtremeRoleId.Assassin)
            {
                return ExtremeRolesPlugin.GameDataStore.IsAssassinAssign;
            }
            return false;
        }

    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Confirm))]
    class MeetingHudConfirmPatch
    {
        public static bool Prefix(
            MeetingHud __instance,
            [HarmonyArgument(0)] byte suspectStateIdx)
        {
            if (!ExtremeRolesPlugin.GameDataStore.AssassinMeetingTrigger) { return true; }

            if (CachedPlayerControl.LocalPlayer.PlayerId != ExtremeRolesPlugin.GameDataStore.ExiledAssassinId)
            {
                return false;
            }
            for (int i = 0; i < __instance.playerStates.Length; i++)
            {
                PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                playerVoteArea.ClearButtons();
                playerVoteArea.voteComplete = true;
            }
            __instance.SkipVoteButton.ClearButtons();
            __instance.SkipVoteButton.voteComplete = true;
            __instance.SkipVoteButton.gameObject.SetActive(false);

            MeetingHud.VoteStates voteStates = __instance.state;
            if (voteStates != MeetingHud.VoteStates.NotVoted)
            {
                return false;
            }
            __instance.state = MeetingHud.VoteStates.Voted;
            __instance.CmdCastVote(PlayerControl.LocalPlayer.PlayerId, suspectStateIdx);

            return false;
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
    class MeetingHudCheckForEndVotingPatch
    {
        public static bool Prefix(
            MeetingHud __instance)
        {
            var gameData = ExtremeRolesPlugin.GameDataStore;

            if (!gameData.AssassinMeetingTrigger) { return true; }

            var (isVoteEnd, voteFor) = assassinVoteState(__instance);

            if (isVoteEnd)
            {
                //GameData.PlayerInfo exiled = Helper.Player.GetPlayerControlById(voteFor).Data;
                Il2CppStructArray<MeetingHud.VoterState> array = 
                    new Il2CppStructArray<MeetingHud.VoterState>(
                        __instance.playerStates.Length);

                if (voteFor == 254 || voteFor == byte.MaxValue)
                {
                    bool targetImposter;
                    do
                    {
                        int randomPlayerIndex = UnityEngine.Random.RandomRange(
                            0, __instance.playerStates.Length);
                        voteFor = __instance.playerStates[randomPlayerIndex].TargetPlayerId;

                        targetImposter = ExtremeRoleManager.GameRole[voteFor].IsImpostor();

                    } while (targetImposter);
                }

                Helper.Logging.Debug($"IsSuccess?:{ExtremeRoleManager.GameRole[voteFor].Id == ExtremeRoleId.Marlin}");

                RPCOperator.Call(
                    CachedPlayerControl.LocalPlayer.PlayerControl.NetId,
                    RPCOperator.Command.AssasinVoteFor,
                    new List<byte> { voteFor });
                RPCOperator.AssasinVoteFor(voteFor);

                for (int i = 0; i < __instance.playerStates.Length; i++)
                {
                    PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                    if (playerVoteArea.TargetPlayerId == ExtremeRolesPlugin.GameDataStore.ExiledAssassinId)
                    {
                        playerVoteArea.VotedFor = voteFor;
                    }
                    else
                    {
                        playerVoteArea.VotedFor = 254;
                    }
                    __instance.SetDirtyBit(1U);

                    array[i] = new MeetingHud.VoterState
                    {
                        VoterId = playerVoteArea.TargetPlayerId,
                        VotedForId = playerVoteArea.VotedFor
                    };

                }
                __instance.RpcVotingComplete(array, null, true);
            }

            return false;
        }

        private static Tuple<bool, byte> assassinVoteState(MeetingHud __instance)
        {
            bool isVoteEnd = false;
            byte voteFor = byte.MaxValue;

            for (int i = 0; i < __instance.playerStates.Length; i++)
            {
                PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                if (playerVoteArea.TargetPlayerId == ExtremeRolesPlugin.GameDataStore.ExiledAssassinId)
                {
                    isVoteEnd = playerVoteArea.DidVote;
                    voteFor = playerVoteArea.VotedFor;
                    break;
                }
            }

            return Tuple.Create(isVoteEnd, voteFor);
        }

    }


    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Select))]
    class MeetingHudSelectPatch
    {
        public static bool Prefix(
            MeetingHud __instance,
            ref bool __result,
            [HarmonyArgument(0)] int suspectStateIdx)
        {
            __result = false;

            if (OptionHolder.Ship.DisableSelfVote &&
                CachedPlayerControl.LocalPlayer.PlayerId == suspectStateIdx)
            {
                return false;
            }
            if (OptionHolder.Ship.BlockSkippingInEmergencyMeeting && suspectStateIdx == -1)
            {
                return false;
            }

            if (!ExtremeRolesPlugin.GameDataStore.AssassinMeetingTrigger) { return true; }


            if (__instance.discussionTimer < (float)PlayerControl.GameOptions.DiscussionTime)
            {
                return __result;
            }
            if (CachedPlayerControl.LocalPlayer.PlayerId != ExtremeRolesPlugin.GameDataStore.ExiledAssassinId)
            {
                return __result;
            }
            SoundManager.Instance.PlaySound(__instance.VoteSound, false, 1f).volume = 0.8f;
            for (int i = 0; i < __instance.playerStates.Length; i++)
            {
                PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                if (suspectStateIdx != (int)playerVoteArea.TargetPlayerId)
                {
                    playerVoteArea.ClearButtons();
                }
            }
            if (suspectStateIdx != -1)
            {
                __instance.SkipVoteButton.ClearButtons();
            }

            __result = true;
            return false;

        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.SetForegroundForDead))]
    class MeetingHudSetForegroundForDeadPatch
    {
        public static bool Prefix(
            MeetingHud __instance)
        {

            if (!ExtremeRolesPlugin.GameDataStore.AssassinMeetingTrigger) { return true; }

            if (ExtremeRoleManager.GetLocalPlayerRole().Id != ExtremeRoleId.Assassin)
            { 
                return true; 
            }
            else
            {
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
    class MeetingHudClosePatch
    {
        public static void Prefix(
            MeetingHud __instance)
        {
            ExtremeRolesPlugin.Info.HideInfoOverlay();
        }
    }


    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CoIntro))]
    class MeetingHudCoIntroPatch
    {
        public static void Postfix(
            MeetingHud __instance,
            [HarmonyArgument(0)] GameData.PlayerInfo reporter,
            [HarmonyArgument(1)] GameData.PlayerInfo reportedBody)
        {
            if (ExtremeRoleManager.GameRole.Count == 0) { return; }

            if (!ExtremeRolesPlugin.GameDataStore.AssassinMeetingTrigger)
            {
                var hockRole = ExtremeRoleManager.GetLocalPlayerRole() as IRoleReportHock;

                if (hockRole != null)
                {
                    var player = CachedPlayerControl.LocalPlayer;

                    if (reportedBody == null)
                    {
                        hockRole.HockReportButton(
                            player, reporter);
                    }
                    else
                    {
                        hockRole.HockBodyReport(
                            player, reporter, reportedBody);
                    }
                }

            }
            else
            {
                __instance.TitleText.text = Helper.Translation.GetString("whoIsMarine");
            }
        }
    }


    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class MeetingHudStartPatch
    {
        public static void Postfix(
            MeetingHud __instance)
        {
            ExtremeRolesPlugin.Info.MeetingStartRest();
            ExtremeRolesPlugin.GameDataStore.ClearMeetingResetObject();
            Helper.Player.ResetTarget();

            if (ExtremeRoleManager.GameRole.Count == 0) { return; }

            var role = ExtremeRoleManager.GetLocalPlayerRole();

            var abilityRole = role as IRoleAbility;
            if (abilityRole != null)
            {
                abilityRole.ResetOnMeetingStart();
            }

            var resetRole = role as IRoleResetMeeting;
            if (resetRole != null)
            {
                resetRole.ResetOnMeetingStart();
            }

            var multiAssignRole = role as Roles.API.MultiAssignRoleBase;
            if (multiAssignRole != null)
            {
                if (multiAssignRole.AnotherRole != null)
                {
                    abilityRole = multiAssignRole.AnotherRole as IRoleAbility;
                    if (abilityRole != null)
                    {
                        abilityRole.ResetOnMeetingStart();
                    }

                    resetRole = multiAssignRole.AnotherRole as IRoleResetMeeting;
                    if (resetRole != null)
                    {
                        resetRole.ResetOnMeetingStart();
                    }
                }
            }

            var ghostRole = ExtremeGhostRoleManager.GetLocalPlayerGhostRole();
            if (ghostRole != null)
            {
                if (ghostRole.Button != null)
                {
                    ghostRole.Button.SetActive(false);
                    ghostRole.Button.ForceAbilityOff();
                }
                ghostRole.ReseOnMeetingStart();
            }

            if (!ExtremeRolesPlugin.GameDataStore.AssassinMeetingTrigger) { return; }

            FastDestroyableSingleton<HudManager>.Instance.Chat.gameObject.SetActive(false);

        }
    }


    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    class MeetingHudUpdatePatch
    {
        public static void Postfix(MeetingHud __instance)
        {

            if (NamePlateHelper.NameplateChange)
            {
                foreach (var pva in __instance.playerStates)
                {
                    NamePlateHelper.UpdateNameplate(pva);
                }
                NamePlateHelper.NameplateChange = false;
            }

            // From TOR
            // This fixes a bug with the original game where pressing the button and a kill happens simultaneously
            // results in bodies sometimes being created *after* the meeting starts, marking them as dead and
            // removing the corpses so there's no random corpses leftover afterwards

            foreach (DeadBody b in UnityEngine.Object.FindObjectsOfType<DeadBody>())
            {
                foreach (PlayerVoteArea pva in __instance.playerStates)
                {
                    if (pva.TargetPlayerId == b.ParentId && !pva.AmDead)
                    {
                        pva.SetDead(pva.DidReport, true);
                        pva.Overlay.gameObject.SetActive(true);
                    }
                }
                UnityEngine.Object.Destroy(b.gameObject);
            }


            if (__instance.state == MeetingHud.VoteStates.Animating) { return; }

            // Deactivate skip Button if skipping on emergency meetings is disabled
            if (OptionHolder.Ship.BlockSkippingInEmergencyMeeting)
            {
                __instance.SkipVoteButton.gameObject.SetActive(false);
            }

            if (ExtremeRolesPlugin.GameDataStore.AssassinMeetingTrigger)
            {
                __instance.TitleText.text = Helper.Translation.GetString(
                    "whoIsMarine");
                __instance.SkipVoteButton.gameObject.SetActive(false);

                if (CachedPlayerControl.LocalPlayer.PlayerId == ExtremeRolesPlugin.GameDataStore.ExiledAssassinId ||
                    ExtremeRoleManager.GetLocalPlayerRole().IsImpostor())
                {
                    FastDestroyableSingleton<HudManager>.Instance.Chat.gameObject.SetActive(true);
                }
                else
                {
                    FastDestroyableSingleton<HudManager>.Instance.Chat.gameObject.SetActive(false);
                }

                return;
            }
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.UpdateButtons))]
    class MeetingHudUpdateButtonsPatch
    {
        public static bool Prefix(MeetingHud __instance)
        {
            if (!ExtremeRolesPlugin.GameDataStore.AssassinMeetingTrigger) { return true; }

            if (AmongUsClient.Instance.AmHost)
            {
                for (int i = 0; i < __instance.playerStates.Length; i++)
                {
                    PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                    GameData.PlayerInfo playerById = GameData.Instance.GetPlayerById(
                        playerVoteArea.TargetPlayerId);
                    if (playerById == null)
                    {
                        playerVoteArea.SetDisabled();
                    }
                }
            }

            return false;
        }
        
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.VotingComplete))]
    public class MeetingHudVotingCompletedPatch
    {
        public static void Postfix(
            MeetingHud __instance,
            [HarmonyArgument(0)] byte[] states,
            [HarmonyArgument(1)] GameData.PlayerInfo exiled,
            [HarmonyArgument(2)] bool tie)
        {
            ExtremeRolesPlugin.Info.HideInfoOverlay();


            foreach (DeadBody b in UnityEngine.Object.FindObjectsOfType<DeadBody>())
            {
                UnityEngine.Object.Destroy(b.gameObject);
            }
        }
    }

}
