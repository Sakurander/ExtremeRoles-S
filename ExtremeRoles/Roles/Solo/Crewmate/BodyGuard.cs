﻿using System.Collections.Generic;

using ExtremeRoles.Helper;
using ExtremeRoles.Module;
using ExtremeRoles.Module.AbilityButton.Roles;
using ExtremeRoles.Resources;
using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.API.Interface;
using ExtremeRoles.Performance;

namespace ExtremeRoles.Roles.Solo.Crewmate
{
    public sealed class BodyGuard : SingleRoleBase, IRoleAbility
    {
        public enum BodyGuardOption
        {
            ShieldRange
        }

        public RoleAbilityButtonBase Button
        {
            get => this.shieldButton;
            set
            {
                this.shieldButton = value;
            }
        }

        public byte TargetPlayer = byte.MaxValue;

        private int shildNum;
        private float shieldRange;
        private RoleAbilityButtonBase shieldButton;

        public BodyGuard() : base(
            ExtremeRoleId.BodyGuard,
            ExtremeRoleType.Crewmate,
            ExtremeRoleId.BodyGuard.ToString(),
            ColorPalette.BodyGuardOrange,
            false, true, false, false)
        { }

        public override void ExiledAction(GameData.PlayerInfo rolePlayer)
        {
            RPCOperator.BodyGuardResetShield(
                    rolePlayer.PlayerId);
        }


        public override void RolePlayerKilledAction(
            PlayerControl rolePlayer, PlayerControl killerPlayer)
        {
            RPCOperator.BodyGuardResetShield(
                rolePlayer.PlayerId);

            if (rolePlayer.PlayerId == killerPlayer.PlayerId) { return; }

            ExtremeRolesPlugin.GameDataStore.ReplaceDeadReason(
                rolePlayer.PlayerId,
                GameDataContainer.PlayerStatus.Martyrdom);
        }

        public void CreateAbility()
        {
            this.CreateAbilityCountButton(
                Translation.GetString("shield"),
                Loader.CreateSpriteFromResources(
                    Path.BodyGuardShield));
            this.Button.SetLabelToCrewmate();
        }

        public bool UseAbility()
        {

            byte playerId = CachedPlayerControl.LocalPlayer.PlayerId;

            RPCOperator.Call(
                CachedPlayerControl.LocalPlayer.PlayerControl.NetId,
                RPCOperator.Command.BodyGuardFeatShield,
                new List<byte>
                {
                    playerId,
                    this.TargetPlayer 
                });
            RPCOperator.BodyGuardFeatShield(
                playerId, this.TargetPlayer);
            this.TargetPlayer = byte.MaxValue;

            return true;
        }

        public bool IsAbilityUse()
        {

            this.TargetPlayer = byte.MaxValue;

            PlayerControl target = Player.GetClosestPlayerInRange(
                CachedPlayerControl.LocalPlayer, this,
                this.shieldRange);

            if (target != null)
            {
                byte targetId = target.PlayerId;

                if (!ExtremeRolesPlugin.GameDataStore.ShildPlayer.IsShielding(
                        CachedPlayerControl.LocalPlayer.PlayerId, targetId))
                {
                    this.TargetPlayer = targetId;
                }
            }

            return this.IsCommonUse() && this.TargetPlayer != byte.MaxValue;
        }

        public void RoleAbilityResetOnMeetingStart()
        {
            RPCOperator.Call(
                CachedPlayerControl.LocalPlayer.PlayerControl.NetId,
                RPCOperator.Command.BodyGuardResetShield,
                new List<byte>
                {
                    CachedPlayerControl.LocalPlayer.PlayerId
                });
            RPCOperator.BodyGuardResetShield(
                CachedPlayerControl.LocalPlayer.PlayerId);

            if (this.shieldButton == null) { return; }

            ((AbilityCountButton)this.shieldButton).UpdateAbilityCount(
                this.shildNum);
        }

        public void RoleAbilityResetOnMeetingEnd()
        {
            return;
        }

        protected override void CreateSpecificOption(
            IOption parentOps)
        {

            CreateFloatOption(
                BodyGuardOption.ShieldRange,
                1.0f, 0.0f, 2.0f, 0.1f,
                parentOps);

            this.CreateAbilityCountOption(
                parentOps, 2, 5);
        }

        protected override void RoleSpecificInit()
        {
            this.shieldRange = OptionHolder.AllOption[
                GetRoleOptionId(BodyGuardOption.ShieldRange)].GetValue();

            this.RoleAbilityInit();
            if (this.Button != null)
            {
                this.shildNum = ((AbilityCountButton)this.shieldButton).CurAbilityNum;
            }
        }
    }
}
