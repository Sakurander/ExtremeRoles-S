﻿using System.Collections.Generic;

using Hazel;
using UnityEngine;

using ExtremeRoles.GhostRoles.API;
using ExtremeRoles.Module;

namespace ExtremeRoles.GhostRoles
{
    public sealed class VanillaGhostRoleWrapper : GhostRoleBase
    {
        private RoleTypes vanillaRoleId;

        public VanillaGhostRoleWrapper(
            RoleTypes vanillaRoleId) : base(
                true, Roles.API.ExtremeRoleType.Crewmate,
                ExtremeGhostRoleId.VanillaRole,
                "", Color.white)
        {
            this.vanillaRoleId = vanillaRoleId;
            this.RoleName = vanillaRoleId.ToString();

            switch (vanillaRoleId)
            {
                case RoleTypes.GuardianAngel:
                    this.Task = true;
                    this.TeamType = Roles.API.ExtremeRoleType.Crewmate;
                    this.NameColor = Palette.White;
                    break;
                default:
                    break;
            }
        }

        public override string GetImportantText()
        {
            switch (this.vanillaRoleId)
            {
                case RoleTypes.GuardianAngel:
                    return Helper.Design.ColoedString(
                        this.NameColor,
                        $"{this.GetColoredRoleName()}: {Helper.Translation.GetString("crewImportantText")}");
                default:
                    return string.Empty;
            }
        }

        public override string GetFullDescription()
        {
            return Helper.Translation.GetString(
                $"{this.vanillaRoleId}FullDescription");
        }


        public override HashSet<Roles.ExtremeRoleId> GetRoleFilter() => new HashSet<Roles.ExtremeRoleId> ();

        public override void ReseOnMeetingEnd()
        {
            return;
        }

        public override void ReseOnMeetingStart()
        {
            return;
        }

        public override void CreateAbility()
        {
            throw new System.Exception("Don't call this class method!!");
        }

        public override void Initialize()
        {
            return;
        }

        protected override void CreateSpecificOption(
            IOption parentOps)
        {
            throw new System.Exception("Don't call this class method!!");
        }

        protected override void UseAbility(
            MessageWriter writer)
        {
            throw new System.Exception("Don't call this class method!!");
        }
    }
}
