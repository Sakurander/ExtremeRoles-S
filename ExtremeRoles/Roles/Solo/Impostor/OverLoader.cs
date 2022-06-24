﻿using System.Collections.Generic;

using ExtremeRoles.Helper;
using ExtremeRoles.Module;
using ExtremeRoles.Module.AbilityButton.Roles;
using ExtremeRoles.Resources;
using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.API.Interface;
using ExtremeRoles.Performance;


namespace ExtremeRoles.Roles.Solo.Impostor
{
    public class OverLoader : SingleRoleBase, IRoleAbility
    {

        public enum OverLoaderOption
        {
            KillCoolReduceRate,
            MoveSpeed
        }

        public bool IsOverLoad;

        private float reduceRate;
        private float defaultKillCool;
        private int defaultKillRange;


        public RoleAbilityButtonBase Button
        {
            get => this.overLoadButton;
            set
            {
                this.overLoadButton = value;
            }
        }

        private RoleAbilityButtonBase overLoadButton;


        public OverLoader() : base(
            ExtremeRoleId.OverLoader,
            ExtremeRoleType.Impostor,
            ExtremeRoleId.OverLoader.ToString(),
            Palette.ImpostorRed,
            true, false, true, true)
        {
            this.IsOverLoad = false;
        }
        
        public static void SwitchAbility(byte rolePlayerId, bool activate)
        {
            var overLoader = ExtremeRoleManager.GetSafeCastedRole<OverLoader>(rolePlayerId);
            if (overLoader != null)
            {
                overLoader.IsOverLoad = activate;
                overLoader.IsBoost = activate;
            }
        }

        public void CreateAbility()
        {
            this.CreatePassiveAbilityButton(
                Translation.GetString("overLoad"),
                Translation.GetString("downLoad"),
                Loader.CreateSpriteFromResources(
                   Path.OverLoaderOverLoad),
                Loader.CreateSpriteFromResources(
                   Path.OverLoaderDownLoad),
                this.CleanUp);
        }

        public bool IsAbilityUse() => this.IsCommonUse();

        public void RoleAbilityResetOnMeetingEnd()
        {
            return;
        }

        public void RoleAbilityResetOnMeetingStart()
        {
            CleanUp();
        }

        public bool UseAbility()
        {
            this.KillCoolTime = this.defaultKillCool * ((100f - this.reduceRate) / 100f);
            this.KillRange = 2;
            abilityOn();
            return true;
        }

        public void CleanUp()
        {
            this.KillCoolTime = this.defaultKillCool;
            this.KillRange = this.defaultKillRange;
            abilityOff();
        }

        protected override void CreateSpecificOption(
            CustomOptionBase parentOps)
        {
            this.CreateCommonAbilityOption(
                parentOps, 7.5f);

            CreateIntOption(
                OverLoaderOption.KillCoolReduceRate,
                75, 50, 90, 1, parentOps,
                format: OptionUnit.Percentage);
            CreateFloatOption(
                OverLoaderOption.MoveSpeed,
                1.5f, 1.0f, 3.0f, 0.1f, parentOps,
                format: OptionUnit.Multiplier);
        }

        protected override void RoleSpecificInit()
        {
            if (!this.HasOtherKillCool)
            {
                this.HasOtherKillCool = true;
                this.KillCoolTime = PlayerControl.GameOptions.KillCooldown;
            }
            if (!this.HasOtherKillRange)
            {
                this.HasOtherKillRange = true;
                this.KillRange = PlayerControl.GameOptions.KillDistance;
            }

            this.defaultKillCool = this.KillCoolTime;
            this.defaultKillRange = this.KillRange;
            this.IsOverLoad = false;

            var allOption = OptionHolder.AllOption;

            this.MoveSpeed = allOption[
                GetRoleOptionId(OverLoaderOption.MoveSpeed)].GetValue();
            this.reduceRate = allOption[
                GetRoleOptionId(OverLoaderOption.KillCoolReduceRate)].GetValue();

            this.RoleAbilityInit();
        }

        private void abilityOn()
        {
            RPCOperator.Call(
                CachedPlayerControl.LocalPlayer.PlayerControl.NetId,
                RPCOperator.Command.OverLoaderSwitchAbility,
                new List<byte> { CachedPlayerControl.LocalPlayer.PlayerId, byte.MaxValue });
            SwitchAbility(
                CachedPlayerControl.LocalPlayer.PlayerId, true);

        }
        private void abilityOff()
        {
            RPCOperator.Call(
                CachedPlayerControl.LocalPlayer.PlayerControl.NetId,
                RPCOperator.Command.OverLoaderSwitchAbility,
                new List<byte> { CachedPlayerControl.LocalPlayer.PlayerId, 0 });
            SwitchAbility(
                CachedPlayerControl.LocalPlayer.PlayerId, false);
        }

    }
}
