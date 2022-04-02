﻿using UnityEngine;
using HarmonyLib;

using ExtremeSkins.Module;

namespace ExtremeSkins.Patches.AmongUs
{
#if WITHHAT
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleAnimation))]
    public static class PlayerPhysicsHandleAnimationPatch
    {
        public static void Postfix(PlayerPhysics __instance)
        {
            AnimationClip currentAnimation = __instance.Animator.GetCurrentAnimation();
            if (currentAnimation == __instance.ClimbAnim || currentAnimation == __instance.ClimbDownAnim) { return; }

            HatParent hp = __instance.myPlayer.HatRenderer;
            
            if (hp.Hat == null) { return; }

            CustomHat hat;
            bool result = ExtremeHatManager.HatData.TryGetValue(
                hp.Hat.ProductId, out hat);

            if (!result) { return; }


            if (hat.HasFrontFlip)
            {
                if (__instance.rend.flipX)
                {
                    hp.FrontLayer.sprite = hat.GetFlipFrontImage();
                }
                else
                {
                    hp.FrontLayer.sprite = hp.Hat.MainImage;
                }
            }
            if (hat.HasBackFlip)
            {
                if (__instance.rend.flipX)
                {
                    hp.BackLayer.sprite = hat.GetBackFlipImage();
                }
                else
                {
                    hp.BackLayer.sprite = hp.Hat.BackImage;
                }
            }
        }
    }
#endif
}
