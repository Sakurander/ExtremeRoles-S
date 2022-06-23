﻿using HarmonyLib;

using ExtremeRoles.Patches.Option;

namespace ExtremeRoles.Patches
{
    [HarmonyPatch(typeof(LanguageSetter), nameof(LanguageSetter.SetLanguage))]
    class SetLanguagepPatch
    {
        static void Postfix()
        {
            OptionsMenuBehaviourStartPatch.UpdateMenuTranslation();
            Compat.CompatModMenu.UpdateTranslation();
        }
    }
}
