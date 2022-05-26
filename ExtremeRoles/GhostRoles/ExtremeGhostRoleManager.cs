﻿using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

using ExtremeRoles.Roles;
using ExtremeRoles.Roles.API;
using ExtremeRoles.GhostRoles.API;

namespace ExtremeRoles.GhostRoles
{
    public enum ExtremeGhostRoleId : byte
    {
        VanillaRole = 0,
    }

    public static class ExtremeGhostRoleManager
    {
        public const int GhostRoleOptionId = 25;

        public class GhostRoleAssignData
        {
            private Dictionary<ExtremeRoleType, int> globalSpawnLimit;

            private Dictionary<ExtremeRoleId, CombinationRoleType> CombRole = new Dictionary<
                ExtremeRoleId, CombinationRoleType>();

            // フィルター、スポーン数、スポーンレート、役職ID
            private Dictionary<ExtremeRoleType, List<(HashSet<ExtremeRoleId>, int, int, ExtremeGhostRoleId)>> useGhostRole = new Dictionary<
                ExtremeRoleType, List<(HashSet<ExtremeRoleId>, int, int, ExtremeGhostRoleId)>>();

            public GhostRoleAssignData()
            {
                this.Clear();
            }

            public void Clear()
            {
                this.globalSpawnLimit.Clear();
                this.useGhostRole.Clear();
            }

            public CombinationRoleType GetCombRoleType(ExtremeRoleId roleId) => CombRole[roleId];

            public int GetGlobalSpawnLimit(ExtremeRoleType team)
            {
                if (this.globalSpawnLimit.ContainsKey(team))
                {
                    return this.globalSpawnLimit[team];
                }
                else
                {
                    return int.MinValue;
                }
            }

            public List<(HashSet<ExtremeRoleId>, int, int, ExtremeGhostRoleId)> GetUseGhostRole(
                ExtremeRoleType team)
            {
                if (this.useGhostRole.ContainsKey(team))
                {
                    return this.useGhostRole[team].OrderBy(
                        item => RandomGenerator.Instance.Next()).ToList();
                }
                else
                {
                    throw new System.Exception("Unknown teamType detect!!");
                }
            }

            public bool IsCombRole(ExtremeRoleId roleId) => this.CombRole.ContainsKey(roleId);

            public bool IsGlobalSpawnLimit(ExtremeRoleType team)
            {
                if (this.globalSpawnLimit.ContainsKey(team))
                {
                    return this.globalSpawnLimit[team] <= 0;
                }
                else
                {
                    throw new System.Exception("Unknown teamType detect!!");
                }
            }

            public void SetCombRoleAssignData(Dictionary<ExtremeRoleId, CombinationRoleType> useGhostCombRole)
            {
                this.CombRole = useGhostCombRole;
            }

            public void SetGlobalSpawnLimit(int crewNum, int impNum, int neutralNum)
            {
                this.globalSpawnLimit.Add(ExtremeRoleType.Crewmate, crewNum);
                this.globalSpawnLimit.Add(ExtremeRoleType.Impostor, impNum);
                this.globalSpawnLimit.Add(ExtremeRoleType.Neutral, neutralNum);
            }
		    public void SetNormalRoleAssignData(
                ExtremeRoleType team,
                HashSet<ExtremeRoleId> filter,
                int spawnNum,
                int spawnRate,
                ExtremeGhostRoleId ghostRoleId)
            {

                (HashSet<ExtremeRoleId>, int, int, ExtremeGhostRoleId) addData = (
                    filter, spawnNum, spawnRate, ghostRoleId);

                if (this.useGhostRole.ContainsKey(team))
                {
                    List<(HashSet<ExtremeRoleId>, int, int, ExtremeGhostRoleId)> teamGhostRole = new List<(HashSet<ExtremeRoleId>, int, int, ExtremeGhostRoleId)>()
                    {
                        addData,
                    };

                    this.useGhostRole.Add(team, teamGhostRole);
                }
                else
                {
                    this.useGhostRole[team].Add(addData);
                }
            }

            public void ReduceGlobalSpawnLimit(ExtremeRoleType team)
            {
                this.globalSpawnLimit[team] = this.globalSpawnLimit[team] - 1;
            }

            public void ReduceRoleSpawnData(
                ExtremeRoleType team,
                HashSet<ExtremeRoleId> filter,
                int spawnNum,
                int spawnRate,
                ExtremeGhostRoleId ghostRoleId)
            {
                int index = this.useGhostRole[team].FindIndex(
                    x => x == (filter, spawnNum, spawnRate, ghostRoleId));

                this.useGhostRole[team][index] = (filter, spawnNum - 1, spawnRate, ghostRoleId);

            }

        }

        public static ConcurrentDictionary<byte, GhostRoleBase> GameRole = new ConcurrentDictionary<byte, GhostRoleBase>();

        public static readonly Dictionary<
            ExtremeGhostRoleId, GhostRoleBase> AllGhostRole = new Dictionary<ExtremeGhostRoleId, GhostRoleBase>()
        { 
        };

        private static readonly HashSet<RoleTypes> vanillaGhostRole = new HashSet<RoleTypes>()
        { 
            RoleTypes.GuardianAngel
        };

        private static GhostRoleAssignData assignData;

        public static void AssignGhostRoleToPlayer(PlayerControl player)
        {
            RoleTypes roleType = player.Data.Role.Role;

            if (vanillaGhostRole.Contains(roleType))
            {
                rpcSetGhostRoleToPlayerId(
                    player, roleType,
                    ExtremeGhostRoleId.VanillaRole);
                return;
            }

            var baseRole = ExtremeRoleManager.GameRole[player.PlayerId];

            ExtremeRoleType team = baseRole.Team;
            ExtremeRoleId roleId = baseRole.Id;

            if (assignData.IsGlobalSpawnLimit(team)) { return; };

            if (assignData.IsCombRole(roleId))
            {
                CombinationRoleType combRoleId = assignData.GetCombRoleType(roleId);
                
                // 専用のコンビ役職を取ってくる
                var ghostCombManager = ExtremeRoleManager.CombRole[(byte)combRoleId];

                assignData.ReduceGlobalSpawnLimit(team);
                return;
            }

            // 各陣営の役職データを取得する
            List<(HashSet<ExtremeRoleId>, int, int, ExtremeGhostRoleId)> sameTeamRoleAssignData = assignData.GetUseGhostRole(
                baseRole.Team);

            foreach (var(filter, num, spawnRate, id) in sameTeamRoleAssignData)
            {
                if (filter.Count != 0 && !filter.Contains(baseRole.Id)) { continue; }
                if (isRoleSpawn(num, spawnRate)) { continue; }
                
                rpcSetGhostRoleToPlayerId(player, roleType, id);
                
                // その役職のスポーン数をへらす処理
                assignData.ReduceRoleSpawnData(
                    team, filter, num, spawnRate, id);
                // 全体の役職減少処理
                assignData.ReduceGlobalSpawnLimit(team);
     
                return;
            }
        }

        public static void CreateGhostRoleOption(int optionIdOffset)
        {
            IEnumerable<GhostRoleBase> roles = AllGhostRole.Values;

            if (roles.Count() == 0) { return; };

            int roleOptionOffset = 0;

            foreach (var item in roles.Select(
                (Value, Index) => new { Value, Index }))
            {
                roleOptionOffset = optionIdOffset + (GhostRoleOptionId * item.Index);
                item.Value.CreateRoleAllOption(roleOptionOffset);
            }

        }

        public static void CreateGhostRoleAssignData(
            Dictionary<ExtremeRoleId, CombinationRoleType> useGhostCombRole)
        {
            var allOption = OptionHolder.AllOption;

            assignData.SetGlobalSpawnLimit(
                UnityEngine.Random.RandomRange(
                    allOption[(int)OptionHolder.CommonOptionKey.MinCrewmateGhostRoles].GetValue(),
                    allOption[(int)OptionHolder.CommonOptionKey.MaxCrewmateGhostRoles].GetValue()),
                UnityEngine.Random.RandomRange(
                    allOption[(int)OptionHolder.CommonOptionKey.MinImpostorGhostRoles].GetValue(),
                    allOption[(int)OptionHolder.CommonOptionKey.MaxImpostorGhostRoles].GetValue()),
                UnityEngine.Random.RandomRange(
                    allOption[(int)OptionHolder.CommonOptionKey.MinNeutralGhostRoles].GetValue(),
                    allOption[(int)OptionHolder.CommonOptionKey.MaxNeutralGhostRoles].GetValue()));

            foreach (var role in AllGhostRole.Values)
            {
                int spawnRate = computePercentage(allOption[
                    role.GetRoleOptionId(RoleCommonOption.SpawnRate)]);
                int roleNum = allOption[
                    role.GetRoleOptionId(RoleCommonOption.RoleNum)].GetValue();

                Helper.Logging.Debug(
                    $"GhostRole Name:{role.Name}  SpawnRate:{spawnRate}   RoleNum:{roleNum}");

                if (roleNum <= 0 || spawnRate <= 0.0)
                {
                    continue;
                }

                assignData.SetNormalRoleAssignData(
                    role.Team,
                    role.GetRoleFilter(),
                    roleNum, spawnRate, role.Id);
            }
        }

        public static GhostRoleBase GetLocalPlayerGhostRole()
        {
            byte playerId = PlayerControl.LocalPlayer.PlayerId;

            if (!GameRole.ContainsKey(playerId))
            {
                return null;
            }
            else
            {
                return GameRole[playerId];
            }
        }
        public static T GetSafeCastedGhostRole<T>(byte playerId) where T : GhostRoleBase
        {
            if (!GameRole.ContainsKey(playerId)) { return null; }

            var role = GameRole[playerId] as T;

            if (role != null)
            {
                return role;
            }

            return null;

        }

        public static T GetSafeCastedLocalPlayerRole<T>() where T : GhostRoleBase
        {

            byte playerId = PlayerControl.LocalPlayer.PlayerId;

            if (!GameRole.ContainsKey(playerId)) { return null; }

            var role = GameRole[playerId] as T;

            if (role != null)
            {
                return role;
            }

            return null;

        }

        public static bool IsGlobalSpawnLimit(ExtremeRoleType team) => assignData.IsGlobalSpawnLimit(team);

        public static void Initialize()
        {
            GameRole.Clear();
            foreach (var role in AllGhostRole.Values)
            {
                role.Initialize();
            }

            if (assignData == null)
            {
                assignData = new GhostRoleAssignData();
            }
            assignData.Clear();
        }

        public static void SetGhostRoleToPlayerId(
            byte playerId, byte vanillaRoleId, byte roleId)
        {

            if (GameRole.ContainsKey(playerId)) { return; }

            RoleTypes roleType = (RoleTypes)vanillaRoleId;
            ExtremeGhostRoleId ghostRoleId = (ExtremeGhostRoleId)roleId;

            if (vanillaGhostRole.Contains(roleType) && 
                ghostRoleId == ExtremeGhostRoleId.VanillaRole)
            {
                GameRole[playerId] = new VanillaGhostRoleWrapper(roleType);
                return;
            }
            
            GhostRoleBase role = AllGhostRole[ghostRoleId].Clone();
            
            role.Initialize();
            role.CreateAbility();

            GameRole[playerId] = role;

        }

        private static bool isRoleSpawn(
            int roleNum, int spawnRate)
        {
            if (roleNum <= 0) { return false; }
            if (spawnRate < UnityEngine.Random.RandomRange(0, 110)) { return false; }

            return true;
        }

        private static int computePercentage(Module.CustomOptionBase self)
            => (int)System.Decimal.Multiply(self.GetValue(), self.Selections.ToList().Count);


        private static void rpcSetGhostRoleToPlayerId(
            PlayerControl player,
            RoleTypes baseVanillaRoleId,
            ExtremeGhostRoleId assignGhostRoleId)
        {
            RPCOperator.Call(
                player.NetId, RPCOperator.Command.SetGhostRole,
                new List<byte>()
                {
                    player.PlayerId,
                    (byte)baseVanillaRoleId,
                    (byte)assignGhostRoleId
                });
            SetGhostRoleToPlayerId(
                player.PlayerId,
                (byte)baseVanillaRoleId,
                (byte)assignGhostRoleId);
        }
    }
}
